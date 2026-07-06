using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class PlatformRuntimeStatusService : IPlatformRuntimeStatusService
{
    private readonly IConfiguration _configuration;

    public PlatformRuntimeStatusService(IConfiguration configuration) => _configuration = configuration;

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

        return new PlatformRuntimeStatus(
            aiConfigured,
            AiConfigurationReader.ResolveAiProviderLabel(_configuration),
            vectorProvider,
            qaRunner,
            allowMock,
            usesSimulated);
    }
}
