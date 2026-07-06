using EnhancementHub.Infrastructure.Options;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Security;

public static class AiConfigurationReader
{
    public static bool IsAiConfigured(IConfiguration configuration)
    {
        var options = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();
        if (IsAzureProvider(options))
        {
            return !string.IsNullOrWhiteSpace(options.AzureOpenAI.ApiKey)
                && !string.IsNullOrWhiteSpace(options.AzureOpenAI.Endpoint);
        }

        if (!string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]);
    }

    public static string ResolveAiProviderLabel(IConfiguration configuration)
    {
        var options = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();
        if (!IsAiConfigured(configuration))
        {
            return "Not configured (mock analysis)";
        }

        return IsAzureProvider(options) ? "Azure OpenAI" : "OpenAI";
    }

    private static bool IsAzureProvider(AiOptions options) =>
        options.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase)
        || options.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase);
}
