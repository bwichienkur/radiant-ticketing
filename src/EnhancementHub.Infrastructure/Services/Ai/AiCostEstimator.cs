namespace EnhancementHub.Infrastructure.Services.Ai;

internal static class AiCostEstimator
{
    public static decimal Estimate(string model, int promptTokens, int completionTokens)
    {
        var normalized = model.ToLowerInvariant();

        if (normalized.Contains("gpt-4o-mini", StringComparison.Ordinal))
        {
            return (promptTokens * 0.15m / 1_000_000m) + (completionTokens * 0.60m / 1_000_000m);
        }

        if (normalized.Contains("gpt-4o", StringComparison.Ordinal))
        {
            return (promptTokens * 2.50m / 1_000_000m) + (completionTokens * 10.00m / 1_000_000m);
        }

        if (normalized.Contains("gpt-4", StringComparison.Ordinal))
        {
            return (promptTokens * 30.00m / 1_000_000m) + (completionTokens * 60.00m / 1_000_000m);
        }

        return (promptTokens + completionTokens) * 1.00m / 1_000_000m;
    }
}
