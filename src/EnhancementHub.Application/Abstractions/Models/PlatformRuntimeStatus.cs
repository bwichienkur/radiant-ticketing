namespace EnhancementHub.Application.Abstractions.Models;

public sealed record PlatformRuntimeStatus(
    bool AiConfigured,
    string AiProvider,
    string VectorSearchProvider,
    string QaRunner,
    bool AllowMockInProduction,
    bool UsesSimulatedBackends,
    IReadOnlyDictionary<string, bool> FeatureFlags);
