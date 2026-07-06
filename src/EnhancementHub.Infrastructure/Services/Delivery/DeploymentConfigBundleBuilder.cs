using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class DeploymentConfigBundleBuilder : IDeploymentConfigBundleBuilder
{
    public Task<DeploymentConfigBundle> BuildAsync(
        ApplicationDeliveryProfile appProfile,
        TenantDeploymentEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var envKey = environment.EnvironmentType.ToString();
        var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var connectionRefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(appProfile.ConfigTransformsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(appProfile.ConfigTransformsJson);
                if (doc.RootElement.TryGetProperty(envKey, out var envNode)
                    && envNode.TryGetProperty("env", out var envObject))
                {
                    foreach (var property in envObject.EnumerateObject())
                    {
                        envVars[property.Name] = property.Value.ToString();
                    }
                }
            }
            catch (JsonException)
            {
                // validation already warns in profile setup
            }
        }

        if (!string.IsNullOrWhiteSpace(appProfile.ConnectionMappingsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(appProfile.ConnectionMappingsJson);
                if (doc.RootElement.TryGetProperty("mappings", out var mappings))
                {
                    foreach (var mapping in mappings.EnumerateArray())
                    {
                        if (!mapping.TryGetProperty("logicalName", out var logicalNameProp))
                        {
                            continue;
                        }

                        var logicalName = logicalNameProp.GetString();
                        if (string.IsNullOrWhiteSpace(logicalName))
                        {
                            continue;
                        }

                        if (mapping.TryGetProperty("byEnvironment", out var byEnv)
                            && byEnv.TryGetProperty(envKey, out var secretRef))
                        {
                            connectionRefs[logicalName] = secretRef.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        var baseUrl = environment.BaseUrlTemplate;
        var bundle = new DeploymentConfigBundle(
            environment.EnvironmentType,
            environment.Name,
            baseUrl,
            appProfile.ConfigTransformsJson ?? "{}",
            connectionRefs,
            envVars);

        return Task.FromResult(bundle);
    }
}
