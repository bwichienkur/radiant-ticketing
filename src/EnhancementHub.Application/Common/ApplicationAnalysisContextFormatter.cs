using EnhancementHub.Domain.Entities;
using System.Text;

namespace EnhancementHub.Application.Common;

public static class ApplicationAnalysisContextFormatter
{
    public static string? Format(ApplicationProfile? profile, string? deploymentNotes = null)
    {
        var sb = new StringBuilder();

        var resolvedDeploymentNotes = profile?.DeploymentNotes ?? deploymentNotes;
        if (!string.IsNullOrWhiteSpace(resolvedDeploymentNotes))
        {
            sb.AppendLine("Deployment & infrastructure constraints:");
            sb.AppendLine(resolvedDeploymentNotes.Trim());
        }

        if (profile is null)
        {
            return sb.Length == 0 ? null : sb.ToString().Trim();
        }

        if (!string.IsNullOrWhiteSpace(profile.KeyComponents))
        {
            sb.AppendLine();
            sb.AppendLine("Key components (from repository scan):");
            sb.AppendLine(Truncate(profile.KeyComponents, 2000));
        }

        if (!string.IsNullOrWhiteSpace(profile.DatabaseUsage))
        {
            sb.AppendLine();
            sb.AppendLine("Database usage:");
            sb.AppendLine(Truncate(profile.DatabaseUsage, 500));
        }

        if (!string.IsNullOrWhiteSpace(profile.ExternalIntegrations))
        {
            sb.AppendLine();
            sb.AppendLine("External integrations / API surface:");
            sb.AppendLine(Truncate(profile.ExternalIntegrations, 500));
        }

        if (!string.IsNullOrWhiteSpace(profile.InternalDependencies))
        {
            sb.AppendLine();
            sb.AppendLine("Internal dependencies:");
            sb.AppendLine(Truncate(profile.InternalDependencies, 500));
        }

        return sb.Length == 0 ? null : sb.ToString().Trim();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
