namespace EnhancementHub.Tests.Common;

public static class ProductionConfigurationTestDefaults
{
    public static void ApplyProductionBackendDefaults(IDictionary<string, string?> configuration)
    {
        configuration.TryAdd("OpenAI:ApiKey", "sk-production-test-key");
        configuration.TryAdd("VectorSearch:Provider", "PgVector");
        configuration.TryAdd("Delivery:Qa:Runner", "Playwright");
    }
}
