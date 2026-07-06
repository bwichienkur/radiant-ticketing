using System.Net.Http.Json;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class GitHubActionsDeploymentAdapter : IDeploymentAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IGitHubAppCloneService _gitHubApp;
    private readonly ILogger<GitHubActionsDeploymentAdapter> _logger;
    private readonly long? _defaultInstallationId;

    public GitHubActionsDeploymentAdapter(
        IHttpClientFactory httpClientFactory,
        IGitHubAppCloneService gitHubApp,
        IConfiguration configuration,
        ILogger<GitHubActionsDeploymentAdapter> logger)
    {
        _httpClient = httpClientFactory.CreateClient(DependencyInjection.InfrastructureServiceExtensions.GitHubAppHttpClientName);
        _gitHubApp = gitHubApp;
        _logger = logger;
        var installation = configuration["GitHubApp:InstallationId"];
        _defaultInstallationId = long.TryParse(installation, out var parsed) ? parsed : null;
    }

    public CicdProvider Provider => CicdProvider.GitHubActions;

    public bool CanHandle(DeploymentContext context) =>
        context.Provider is CicdProvider.GitHubActions or CicdProvider.Manual;

    public async Task<DeploymentResult> DeployAsync(DeploymentContext context, CancellationToken cancellationToken = default)
    {
        var workflow = context.PipelineReference ?? ".github/workflows/deploy.yml";
        var deploymentRef = $"gha:{workflow}:{context.EnvironmentType}:{Guid.NewGuid():N}";

        if (!_gitHubApp.IsConfigured
            || !_defaultInstallationId.HasValue
            || string.IsNullOrWhiteSpace(context.RepositoryOwner)
            || string.IsNullOrWhiteSpace(context.RepositoryName))
        {
            return new DeploymentResult(
                true,
                deploymentRef,
                context.ConfigBundle.BaseUrl,
                true,
                null);
        }

        try
        {
            var token = await _gitHubApp.GetInstallationAccessTokenAsync(_defaultInstallationId.Value, cancellationToken);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"repos/{context.RepositoryOwner}/{context.RepositoryName}/actions/workflows/{workflow}/dispatches");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("EnhancementHub", "1.0"));
            request.Content = JsonContent.Create(new
            {
                @ref = context.DefaultBranch ?? "main",
                inputs = new
                {
                    environment = context.EnvironmentType.ToString(),
                    requestId = context.EnhancementRequestId.ToString(),
                    config = context.ConfigBundle.ConfigJson
                }
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GitHub Actions dispatch failed ({Status}): {Body}", response.StatusCode, body);
                return new DeploymentResult(
                    true,
                    deploymentRef,
                    context.ConfigBundle.BaseUrl,
                    true,
                    null);
            }

            return new DeploymentResult(true, deploymentRef, context.ConfigBundle.BaseUrl, false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub Actions deploy simulation fallback for request {RequestId}", context.EnhancementRequestId);
            return new DeploymentResult(true, deploymentRef, context.ConfigBundle.BaseUrl, true, null);
        }
    }
}

public sealed class WebhookDeploymentAdapter : IDeploymentAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookDeploymentAdapter> _logger;

    public WebhookDeploymentAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebhookDeploymentAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public CicdProvider Provider => CicdProvider.Webhook;

    public bool CanHandle(DeploymentContext context) => context.Provider == CicdProvider.Webhook;

    public async Task<DeploymentResult> DeployAsync(DeploymentContext context, CancellationToken cancellationToken = default)
    {
        var webhookUrl = context.PipelineReference
            ?? _configuration["Delivery:WebhookUrl"];

        var deploymentRef = $"webhook:{context.EnvironmentType}:{Guid.NewGuid():N}";
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return new DeploymentResult(true, deploymentRef, context.ConfigBundle.BaseUrl, true, null);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                requestId = context.EnhancementRequestId,
                applicationId = context.ApplicationId,
                environment = context.EnvironmentType.ToString(),
                mechanism = context.Mechanism.ToString(),
                config = context.ConfigBundle
            };
            var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new DeploymentResult(false, null, null, false, $"Webhook failed: {body}");
            }

            return new DeploymentResult(true, deploymentRef, context.ConfigBundle.BaseUrl, false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook deploy failed for request {RequestId}", context.EnhancementRequestId);
            return new DeploymentResult(false, null, null, false, ex.Message);
        }
    }
}

public sealed class DeploymentAdapterFactory : IDeploymentAdapterFactory
{
    private readonly IReadOnlyList<IDeploymentAdapter> _adapters;

    public DeploymentAdapterFactory(IEnumerable<IDeploymentAdapter> adapters) =>
        _adapters = adapters.ToList();

    public IDeploymentAdapter Resolve(DeploymentContext context) =>
        _adapters.FirstOrDefault(a => a.CanHandle(context))
        ?? _adapters.First(a => a.Provider == CicdProvider.GitHubActions);
}
