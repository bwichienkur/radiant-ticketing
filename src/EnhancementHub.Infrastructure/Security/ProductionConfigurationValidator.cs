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

        var storageProvider = configuration["DataProtection:StorageProvider"] ?? "FileSystem";
        var keysPath = configuration["DataProtection:KeysPath"];
        var azureBlobConnection = configuration["DataProtection:AzureBlob:ConnectionString"];

        if (string.Equals(storageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(azureBlobConnection))
            {
                throw new InvalidOperationException(
                    "DataProtection:AzureBlob:ConnectionString is required when StorageProvider=AzureBlob in Production.");
            }
        }
        else if (string.IsNullOrWhiteSpace(keysPath))
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath is required in Production (use shared NFS/Azure Files) or set StorageProvider=AzureBlob.");
        }

        if (configuration.GetValue<bool>("Authentication:OpenIdConnect:Enabled"))
        {
            ValidateOpenIdConnectConfiguration(configuration);
        }
    }

    private static void ValidateOpenIdConnectConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication:OpenIdConnect");

        RequireSetting(section, "Authority");
        RequireSetting(section, "ClientId");
        RequireSetting(section, "ClientSecret");

        var authority = section["Authority"]!;
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
            || authorityUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "Authentication:OpenIdConnect:Authority must be a valid absolute URL in Production.");
        }

        var defaultRole = section["DefaultRole"];
        var hasRoleMappings = section.GetSection("RoleMappings").GetChildren().Any();
        if (string.IsNullOrWhiteSpace(defaultRole) && !hasRoleMappings)
        {
            throw new InvalidOperationException(
                "Authentication:OpenIdConnect requires DefaultRole or at least one RoleMappings entry in Production.");
        }
    }

    private static void RequireSetting(IConfiguration section, string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            throw new InvalidOperationException(
                $"Authentication:OpenIdConnect:{key} is required when OpenID Connect is enabled in Production.");
        }
    }
}
