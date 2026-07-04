using System.Text.Json;
using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Infrastructure.Services;

public sealed class AiResponseValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool TryValidate(string json, out AiAnalysisResult? result, out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty AI response.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("summary", out var summaryProp) || summaryProp.ValueKind != JsonValueKind.String)
            {
                error = "Missing or invalid 'summary' property.";
                return false;
            }

            result = JsonSerializer.Deserialize<AiAnalysisResult>(json, JsonOptions);
            if (result is null)
            {
                error = "Failed to deserialize analysis result.";
                return false;
            }

            result.Summary = result.Summary.Trim();
            if (string.IsNullOrWhiteSpace(result.Summary))
            {
                error = "Summary cannot be empty.";
                return false;
            }

            result.ImpactedAreas ??= Array.Empty<string>();
            result.Recommendations ??= Array.Empty<string>();
            result.Risks ??= Array.Empty<string>();
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON: {ex.Message}";
            return false;
        }
    }
}
