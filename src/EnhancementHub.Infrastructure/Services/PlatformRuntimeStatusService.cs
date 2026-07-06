using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class PlatformRuntimeStatusService : IPlatformRuntimeStatusService
{
    private readonly IConfiguration _configuration;
    private readonly IFeatureService _featureService;

    public PlatformRuntimeStatusService(IConfiguration configuration, IFeatureService featureService)
    {
        _configuration = configuration;
        _featureService = featureService;
    }

    public PlatformRuntimeStatus GetStatus()
    {
        var aiConfigured = AiConfigurationReader.IsAiConfigured(_configuration);
        var vectorProvider = _configuration["VectorSearch:Provider"] ?? "InMemory";
        var qaRunner = _configuration.GetValue<string>("Delivery:Qa:Runner") ?? "Playwright";
        var allowMock = _configuration.GetValue("AI:AllowMockInProduction", false);

        var usesSimulated =
            !aiConfigured
            || string.Equals(vectorProvider, "InMemory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(qaRunner, "Simulated", StringComparison.OrdinalIgnoreCase);

        var featureFlags = new Dictionary<string, bool>
        {
            [FeatureFlags.IntakeCopilot] = _featureService.IsEnabled(FeatureFlags.IntakeCopilot),
            [FeatureFlags.GlobalSearch] = _featureService.IsEnabled(FeatureFlags.GlobalSearch),
            [FeatureFlags.FeedbackWidget] = _featureService.IsEnabled(FeatureFlags.FeedbackWidget)
        };

        return new PlatformRuntimeStatus(
            aiConfigured,
            AiConfigurationReader.ResolveAiProviderLabel(_configuration),
            vectorProvider,
            qaRunner,
            allowMock,
            usesSimulated,
            featureFlags);
    }
}
