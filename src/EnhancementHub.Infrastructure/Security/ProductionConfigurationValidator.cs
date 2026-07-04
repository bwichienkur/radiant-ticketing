using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EnhancementHub.Infrastructure.Security;

public static class ProductionConfigurationValidator
{
    public const string DevJwtSecret = "dev-secret-change-in-production-min-32-chars!!";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == DevJwtSecret)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be configured with a strong, unique value in Production.");
        }

        if (jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 characters in Production.");
        }

        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath is required in Production so encrypted secrets survive restarts.");
        }
    }
}
