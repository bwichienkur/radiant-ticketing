using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Observability;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ConfigurationFeatureService : IFeatureService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public ConfigurationFeatureService(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
    }

    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            return false;
        }

        var cacheKey = $"feature:{featureName}";
        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            EnhancementHubTelemetry.FeatureFlagCacheHits.Add(1);
            return cached;
        }

        var enabled = _configuration.GetSection("Features")[featureName] is string raw
            && bool.TryParse(raw, out var parsed)
            && parsed;

        _cache.Set(cacheKey, enabled, CacheTtl);
        EnhancementHubTelemetry.FeatureFlagCacheMisses.Add(1);
        return enabled;
    }
}
