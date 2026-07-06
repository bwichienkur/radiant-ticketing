using System.Text.Json;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Delivery;

public static class DeliveryProfileMapper
{
    public static TenantDeliveryProfileDto ToDto(TenantDeliveryProfile profile) =>
        new(
            profile.Id,
            profile.TenantId,
            profile.DefaultCicdProvider,
            profile.VaultSecretPrefix,
            profile.AutoImplementOnApprove,
            profile.AutoDeployToTest,
            profile.RequirePullRequestReview,
            profile.RequireUatSignoff,
            profile.RequireProdChangeWindow,
            profile.ChangeWindowNotes,
            profile.QaVideoRetentionDays,
            profile.Environments
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Name)
                .Select(e => new TenantDeploymentEnvironmentDto(
                    e.Id,
                    e.Name,
                    e.EnvironmentType,
                    e.BaseUrlTemplate,
                    e.SecretReferencePrefix,
                    e.IsActive,
                    e.SortOrder,
                    e.RequiresApprovalForDeploy))
                .ToList());

    public static ApplicationDeliveryProfileDto ToDto(
        ApplicationDeliveryProfile profile,
        IReadOnlyList<string> validationMessages)
    {
        var isConfigured = DeliveryProfileValidator.ValidateApplicationProfile(profile).IsValid;
        return new ApplicationDeliveryProfileDto(
            profile.Id,
            profile.ApplicationId,
            profile.DeploymentMechanism,
            profile.PrimaryRepositoryId,
            profile.BranchNamingPattern,
            profile.CicdPipelineReference,
            profile.CicdProviderOverride,
            profile.SmokeTestPath,
            profile.DatabaseMigrationStrategy,
            profile.RequiresHumanProdDeploy,
            profile.ConfigTransformsJson,
            profile.ConnectionMappingsJson,
            isConfigured,
            validationMessages);
    }
}

public static class DeliveryProfileValidator
{
    public static DeliveryProfileValidationResultDto ValidateTenantProfile(
        TenantDeliveryProfile? profile,
        IReadOnlyList<TenantDeploymentEnvironment> environments)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (profile is null)
        {
            errors.Add("Tenant delivery profile is not configured.");
            return new DeliveryProfileValidationResultDto(false, errors, warnings);
        }

        if (environments.Count == 0)
        {
            warnings.Add("No deployment environments defined. Add at least a Test environment.");
        }

        if (!environments.Any(e => e.IsActive && e.EnvironmentType == DeploymentEnvironmentType.Test))
        {
            warnings.Add("No active Test environment. Automated QA deploys need a test target.");
        }

        if (profile.AutoDeployToTest && !environments.Any(e => e.IsActive && e.EnvironmentType == DeploymentEnvironmentType.Test))
        {
            errors.Add("Auto-deploy to test is enabled but no active Test environment exists.");
        }

        if (profile.RequireProdChangeWindow && string.IsNullOrWhiteSpace(profile.ChangeWindowNotes))
        {
            warnings.Add("Production change windows are required but no schedule notes are documented.");
        }

        return new DeliveryProfileValidationResultDto(errors.Count == 0, errors, warnings);
    }

    public static DeliveryProfileValidationResultDto ValidateApplicationProfile(ApplicationDeliveryProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.CicdPipelineReference))
        {
            errors.Add("CI/CD pipeline reference is required (workflow path, pipeline ID, or webhook).");
        }

        if (profile.PrimaryRepositoryId is null)
        {
            warnings.Add("No primary repository selected. Implementation will need a repo to branch from.");
        }

        if (string.IsNullOrWhiteSpace(profile.BranchNamingPattern))
        {
            errors.Add("Branch naming pattern is required.");
        }

        ValidateJson(profile.ConfigTransformsJson, "Config transforms", warnings);
        ValidateJson(profile.ConnectionMappingsJson, "Connection mappings", warnings);

        if (profile.DeploymentMechanism == DeploymentMechanism.Custom
            && string.IsNullOrWhiteSpace(profile.CicdPipelineReference))
        {
            warnings.Add("Custom deployment mechanism should include a webhook or pipeline reference.");
        }

        return new DeliveryProfileValidationResultDto(errors.Count == 0, errors, warnings);
    }

    private static void ValidateJson(string? json, string label, ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            warnings.Add($"{label} JSON is not valid.");
        }
    }
}
