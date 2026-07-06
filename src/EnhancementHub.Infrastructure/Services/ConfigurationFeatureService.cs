using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ConfigurationFeatureService : IFeatureService
{
    private readonly IConfiguration _configuration;

    public ConfigurationFeatureService(IConfiguration configuration) =>
        _configuration = configuration;

    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            return false;
        }

        return _configuration.GetSection("Features")[featureName] is string raw
            && bool.TryParse(raw, out var enabled)
            && enabled;
    }
}
