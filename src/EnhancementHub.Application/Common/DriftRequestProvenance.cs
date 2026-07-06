using System.Text.RegularExpressions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Common;

public static partial class DriftRequestProvenance
{
    public const string MarkerPrefix = "[source-drift:";

    public static string FormatMarker(Guid findingId) => $"{MarkerPrefix}{findingId}]";

    public static string BuildSupportingNotes(Guid findingId, string? connectionName, DriftSeverity severity)
    {
        var marker = FormatMarker(findingId);
        var connection = string.IsNullOrWhiteSpace(connectionName) ? "database connection" : connectionName;
        return $"{marker}\nOriginated from schema drift finding ({severity}) on {connection}.";
    }

    public static Guid? TryParseFindingId(string? supportingNotes)
    {
        if (string.IsNullOrWhiteSpace(supportingNotes))
        {
            return null;
        }

        var match = FindingIdRegex().Match(supportingNotes);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out var findingId)
            ? findingId
            : null;
    }

    [GeneratedRegex(@"\[source-drift:([0-9a-fA-F\-]{36})\]")]
    private static partial Regex FindingIdRegex();
}
