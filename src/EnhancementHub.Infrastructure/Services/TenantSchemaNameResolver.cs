using System.Text.RegularExpressions;
using EnhancementHub.Application.Options;

namespace EnhancementHub.Infrastructure.Services;

public static partial class TenantSchemaNameResolver
{
  private static readonly Regex InvalidSchemaChars = InvalidSchemaCharacterPattern();

    public static string BuildSchemaName(string slug, TenantIsolationOptions options)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        normalized = InvalidSchemaChars.Replace(normalized, "_");
        normalized = normalized.Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "default";
        }

        var prefix = options.SchemaPrefix.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "tenant_";
        }

        var schema = prefix + normalized;
        return schema.Length <= 63 ? schema : schema[..63];
    }

    public static bool IsValidSchemaName(string schemaName) =>
        !string.IsNullOrWhiteSpace(schemaName)
        && schemaName.Length <= 63
        && SchemaNamePattern().IsMatch(schemaName);

    [GeneratedRegex(@"[^a-z0-9_]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidSchemaCharacterPattern();

    [GeneratedRegex(@"^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SchemaNamePattern();
}
