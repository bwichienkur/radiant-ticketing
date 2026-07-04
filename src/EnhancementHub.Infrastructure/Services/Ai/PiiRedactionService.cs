using System.Text.RegularExpressions;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Options;
using EnhancementHub.Infrastructure.Options;

namespace EnhancementHub.Infrastructure.Services.Ai;

public sealed class PiiRedactionService : IPiiRedactionService
{
    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"\b(?:\+?\d{1,3}[-.\s]?)?(?:\(?\d{3}\)?[-.\s]?){2}\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex SsnPattern = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardPattern = new(
        @"\b(?:\d[ -]*?){13,19}\b",
        RegexOptions.Compiled);

    private readonly AiOptions _options;

    public PiiRedactionService(IOptions<AiOptions> options) => _options = options.Value;

    public string Redact(string input)
    {
        if (!_options.PiiRedactionEnabled || string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var redacted = EmailPattern.Replace(input, "[REDACTED_EMAIL]");
        redacted = PhonePattern.Replace(redacted, "[REDACTED_PHONE]");
        redacted = SsnPattern.Replace(redacted, "[REDACTED_SSN]");
        redacted = CreditCardPattern.Replace(redacted, "[REDACTED_CARD]");
        return redacted;
    }
}
