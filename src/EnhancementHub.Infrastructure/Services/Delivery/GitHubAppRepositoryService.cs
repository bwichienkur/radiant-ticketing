using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class GitHubAppRepositoryService : IGitHubAppRepositoryService
{
    private readonly HttpClient _httpClient;
    private readonly IGitHubAppCloneService _gitHubApp;
    private readonly ILogger<GitHubAppRepositoryService> _logger;
    private readonly long? _defaultInstallationId;

    public GitHubAppRepositoryService(
        IHttpClientFactory httpClientFactory,
        IGitHubAppCloneService gitHubApp,
        IConfiguration configuration,
        ILogger<GitHubAppRepositoryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(DependencyInjection.InfrastructureServiceExtensions.GitHubAppHttpClientName);
        _gitHubApp = gitHubApp;
        _logger = logger;
        var installation = configuration["GitHubApp:InstallationId"];
        _defaultInstallationId = long.TryParse(installation, out var parsed) ? parsed : null;
    }

    public bool IsConfigured => _gitHubApp.IsConfigured;

    public async Task<GitHubPullRequestResult> CreateImplementationPullRequestAsync(
        string owner,
        string repository,
        string baseBranch,
        string branchName,
        string title,
        string body,
        string implementationMarkdown,
        long? installationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !_defaultInstallationId.HasValue)
        {
            return Simulate(owner, repository, branchName, title);
        }

        try
        {
            var token = await _gitHubApp.GetInstallationAccessTokenAsync(
                installationId ?? _defaultInstallationId.Value,
                cancellationToken);

            var baseSha = await GetBranchShaAsync(owner, repository, baseBranch, token, cancellationToken);
            await CreateBranchAsync(owner, repository, branchName, baseSha, token, cancellationToken);

            var filePath = $"delivery/{branchName.Replace('/', '-')}/IMPLEMENTATION.md";
            var commitSha = await UpsertFileAsync(
                owner,
                repository,
                branchName,
                filePath,
                implementationMarkdown,
                $"Delivery: {title}",
                token,
                cancellationToken);

            var pr = await CreatePullRequestAsync(
                owner,
                repository,
                title,
                body,
                branchName,
                baseBranch,
                token,
                cancellationToken);

            return new GitHubPullRequestResult(
                true,
                branchName,
                pr.HtmlUrl,
                pr.Number,
                commitSha,
                false,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub PR creation failed; falling back to simulation for {Owner}/{Repo}", owner, repository);
            return Simulate(owner, repository, branchName, title);
        }
    }

    public async Task<GitHubFileUpsertResult> UpsertBranchFileAsync(
        string owner,
        string repository,
        string branch,
        string path,
        string content,
        string commitMessage,
        long? installationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !_defaultInstallationId.HasValue)
        {
            return new GitHubFileUpsertResult(true, Guid.NewGuid().ToString("N")[..12], true, null);
        }

        try
        {
            var token = await _gitHubApp.GetInstallationAccessTokenAsync(
                installationId ?? _defaultInstallationId.Value,
                cancellationToken);
            var commitSha = await UpsertFileAsync(
                owner,
                repository,
                branch,
                path,
                content,
                commitMessage,
                token,
                cancellationToken);
            return new GitHubFileUpsertResult(true, commitSha, false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub file upsert failed for {Owner}/{Repo}/{Path}", owner, repository, path);
            return new GitHubFileUpsertResult(false, null, true, ex.Message);
        }
    }

    private GitHubPullRequestResult Simulate(string owner, string repository, string branchName, string title)
    {
        var number = Random.Shared.Next(100, 999);
        return new GitHubPullRequestResult(
            true,
            branchName,
            $"https://github.com/{owner}/{repository}/pull/{number}",
            number,
            Guid.NewGuid().ToString("N")[..12],
            true,
            null);
    }

    private async Task<string> GetBranchShaAsync(
        string owner,
        string repo,
        string branch,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"repos/{owner}/{repo}/git/ref/heads/{branch}", token);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GitRefResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Missing git ref response.");
        return payload.Object.Sha;
    }

    private async Task CreateBranchAsync(
        string owner,
        string repo,
        string branchName,
        string sha,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, $"repos/{owner}/{repo}/git/refs", token);
        request.Content = JsonContent.Create(new
        {
            @ref = $"refs/heads/{branchName}",
            sha
        });
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Create branch failed: {body}");
        }
    }

    private async Task<string> UpsertFileAsync(
        string owner,
        string repo,
        string branch,
        string path,
        string content,
        string message,
        string token,
        CancellationToken cancellationToken)
    {
        string? existingSha = null;
        using (var getRequest = CreateRequest(HttpMethod.Get, $"repos/{owner}/{repo}/contents/{path}?ref={branch}", token))
        {
            var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (getResponse.IsSuccessStatusCode)
            {
                var existing = await getResponse.Content.ReadFromJsonAsync<GitContentResponse>(cancellationToken: cancellationToken);
                existingSha = existing?.Sha;
            }
        }

        using var putRequest = CreateRequest(HttpMethod.Put, $"repos/{owner}/{repo}/contents/{path}", token);
        putRequest.Content = JsonContent.Create(new
        {
            message,
            content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            branch,
            sha = existingSha
        });
        var putResponse = await _httpClient.SendAsync(putRequest, cancellationToken);
        putResponse.EnsureSuccessStatusCode();
        var result = await putResponse.Content.ReadFromJsonAsync<GitContentPutResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Missing content put response.");
        return result.Commit.Sha;
    }

    private async Task<GitPullRequestResponse> CreatePullRequestAsync(
        string owner,
        string repo,
        string title,
        string body,
        string head,
        string @base,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, $"repos/{owner}/{repo}/pulls", token);
        request.Content = JsonContent.Create(new { title, body, head, @base });
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GitPullRequestResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Missing pull request response.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("EnhancementHub", "1.0"));
        return request;
    }

    private sealed class GitRefResponse
    {
        [JsonPropertyName("object")]
        public GitObject Object { get; set; } = new();

        public sealed class GitObject
        {
            [JsonPropertyName("sha")]
            public string Sha { get; set; } = string.Empty;
        }
    }

    private sealed class GitContentResponse
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;
    }

    private sealed class GitContentPutResponse
    {
        [JsonPropertyName("commit")]
        public GitCommit Commit { get; set; } = new();

        public sealed class GitCommit
        {
            [JsonPropertyName("sha")]
            public string Sha { get; set; } = string.Empty;
        }
    }

    private sealed class GitPullRequestResponse
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }
    }
}
