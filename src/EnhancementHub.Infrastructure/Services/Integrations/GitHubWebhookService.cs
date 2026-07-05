using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class GitHubWebhookService : IGitHubWebhookService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly HangfireRepositoryIndexingDispatcher _indexingDispatcher;
    private readonly IntegrationsOptions _options;
    private readonly ILogger<GitHubWebhookService> _logger;

    public GitHubWebhookService(
        IEnhancementHubDbContext dbContext,
        HangfireRepositoryIndexingDispatcher indexingDispatcher,
        IOptions<IntegrationsOptions> options,
        ILogger<GitHubWebhookService> logger)
    {
        _dbContext = dbContext;
        _indexingDispatcher = indexingDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GitHubWebhookResult> HandlePushAsync(
        string payload,
        string? signature256,
        CancellationToken cancellationToken = default)
    {
        if (!_options.GitHub.Enabled)
        {
            return new GitHubWebhookResult(false, 0, "GitHub webhooks are disabled.");
        }

        if (!VerifySignature(payload, signature256, _options.GitHub.WebhookSecret))
        {
            return new GitHubWebhookResult(false, 0, "Invalid webhook signature.");
        }

        var push = JsonSerializer.Deserialize<GitHubPushEvent>(payload);
        if (push?.Repository?.FullName is null)
        {
            return new GitHubWebhookResult(false, 0, "Unsupported webhook payload.");
        }

        var repoKey = push.Repository.FullName.ToLowerInvariant();
        var repositories = await _dbContext.Repositories
            .Where(r => r.AutoIndexOnPush && r.Url.ToLower().Contains(repoKey))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (repositories.Count == 0)
        {
            _logger.LogInformation("GitHub push for {Repo} did not match any registered repositories", repoKey);
            return new GitHubWebhookResult(true, 0, "No matching repositories.");
        }

        await _indexingDispatcher.DispatchAsync(repositories, cancellationToken);
        _logger.LogInformation(
            "GitHub push queued indexing for {Count} repositories matching {Repo}",
            repositories.Count,
            repoKey);

        return new GitHubWebhookResult(true, repositories.Count, null);
    }

    internal static bool VerifySignature(string payload, string? signatureHeader, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)
            || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = signatureHeader["sha256=".Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(expected.ToLowerInvariant()));
    }

    private sealed class GitHubPushEvent
    {
        [JsonPropertyName("repository")]
        public GitHubRepository? Repository { get; set; }
    }

    private sealed class GitHubRepository
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }
    }
}
