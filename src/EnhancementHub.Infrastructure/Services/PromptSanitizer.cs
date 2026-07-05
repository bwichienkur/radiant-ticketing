using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Infrastructure.Services;

public sealed class PromptSanitizer
{
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous instructions",
        "ignore all prior",
        "disregard previous",
        "system prompt",
        "you are now",
        "jailbreak",
        "###",
        "<|im_start|>"
    ];

    private static readonly Regex ControlChars = new(@"[\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    public string SanitizeUserInput(string input, int maxLength = 8000)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = ControlChars.Replace(input, " ").Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        var lower = normalized.ToLowerInvariant();
        foreach (var pattern in InjectionPatterns)
        {
            if (lower.Contains(pattern, StringComparison.Ordinal))
            {
                normalized = normalized.Replace(pattern, "[filtered]", StringComparison.OrdinalIgnoreCase);
            }
        }

        return normalized;
    }

    public string BuildStructuredPrompt(
        string title,
        string description,
        string? repositoryContext,
        string? applicationContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following enhancement request for a software repository.");
        sb.AppendLine("Respond ONLY with valid JSON matching the required schema.");
        sb.AppendLine();
        sb.AppendLine($"Title: {SanitizeUserInput(title, 500)}");
        sb.AppendLine($"Description: {SanitizeUserInput(description)}");

        if (!string.IsNullOrWhiteSpace(applicationContext))
        {
            sb.AppendLine();
            sb.AppendLine("Application & infrastructure context:");
            sb.AppendLine(SanitizeUserInput(applicationContext, 6000));
        }
        else if (!string.IsNullOrWhiteSpace(repositoryContext))
        {
            sb.AppendLine();
            sb.AppendLine("Repository context:");
            sb.AppendLine(SanitizeUserInput(repositoryContext, 4000));
        }

        return sb.ToString();
    }
}
