using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EnhancementHub.Infrastructure.Services;

public sealed class GitHubAppCloneService : IGitHubAppCloneService
{
    private readonly HttpClient _httpClient;
    private readonly IGitRepositoryCloneService _gitCloneService;
    private readonly ILogger<GitHubAppCloneService> _logger;
    private readonly string? _appId;
    private readonly string? _privateKeyPem;
    private readonly long? _defaultInstallationId;

    public GitHubAppCloneService(
        IHttpClientFactory httpClientFactory,
        IGitRepositoryCloneService gitCloneService,
        IConfiguration configuration,
        ILogger<GitHubAppCloneService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(InfrastructureServiceExtensions.GitHubAppHttpClientName);
        _gitCloneService = gitCloneService;
        _logger = logger;
        _appId = configuration["GitHubApp:AppId"];
        _privateKeyPem = ResolvePrivateKey(configuration);
        var installation = configuration["GitHubApp:InstallationId"];
        _defaultInstallationId = long.TryParse(installation, out var parsed) ? parsed : null;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_appId) && !string.IsNullOrWhiteSpace(_privateKeyPem);

    public async Task<GitHubAppCloneResult> CloneRepositoryAsync(
        string owner,
        string repository,
        string branch = "main",
        long? installationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new GitHubAppCloneResult(false, null, "GitHub App is not configured. Set GitHubApp:AppId and GitHubApp:PrivateKey.");
        }

        var resolvedInstallationId = installationId ?? _defaultInstallationId;
        if (!resolvedInstallationId.HasValue)
        {
            return new GitHubAppCloneResult(false, null, "GitHub App installation ID is required.");
        }

        try
        {
            var token = await GetInstallationAccessTokenAsync(resolvedInstallationId.Value, cancellationToken);
            var cloneUrl = $"https://github.com/{owner.Trim()}/{repository.Trim()}.git";
            var clone = await _gitCloneService.CloneAsync(cloneUrl, branch, token, cancellationToken);
            return new GitHubAppCloneResult(clone.Succeeded, clone.LocalPath, clone.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub App clone failed for {Owner}/{Repository}", owner, repository);
            return new GitHubAppCloneResult(false, null, ex.Message);
        }
    }

    public Task<string> GetInstallationAccessTokenAsync(long installationId, CancellationToken cancellationToken = default) =>
        GetInstallationTokenAsync(installationId, cancellationToken);

    internal async Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken)
    {
        var jwt = CreateAppJwt();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"app/installations/{installationId}/access_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("EnhancementHub", "1.0"));

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"GitHub App token request failed ({response.StatusCode}): {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<InstallationTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("GitHub App token response was empty.");

        if (string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("GitHub App token response did not include a token.");
        }

        return payload.Token;
    }

    internal string CreateAppJwt()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKeyPem!);

        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var now = DateTimeOffset.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _appId,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.AddMinutes(-1).UtcDateTime,
            Expires = now.AddMinutes(9).UtcDateTime,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    private static string? ResolvePrivateKey(IConfiguration configuration)
    {
        var inline = configuration["GitHubApp:PrivateKey"];
        if (!string.IsNullOrWhiteSpace(inline))
        {
            return inline.Replace("\\n", "\n", StringComparison.Ordinal);
        }

        var keyPath = configuration["GitHubApp:PrivateKeyPath"];
        if (!string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath))
        {
            return File.ReadAllText(keyPath);
        }

        return null;
    }

    private sealed class InstallationTokenResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
