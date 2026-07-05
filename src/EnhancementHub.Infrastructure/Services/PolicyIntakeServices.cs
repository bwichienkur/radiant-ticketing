using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace EnhancementHub.Infrastructure.Services;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    private const int MaxChars = 50_000;

    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".txt", ".md", ".csv"];

    public Task<DocumentTextExtractionResult> ExtractAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Task.FromResult(new DocumentTextExtractionResult(
                false,
                string.Empty,
                $"Policy documents must be PDF, TXT, or Markdown. Got '{extension}'."));
        }

        return extension switch
        {
            ".pdf" => ExtractPdfAsync(content, cancellationToken),
            _ => ExtractPlainTextAsync(content, cancellationToken)
        };
    }

    private static async Task<DocumentTextExtractionResult> ExtractPlainTextAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new DocumentTextExtractionResult(false, string.Empty, "Document is empty.");
        }

        return new DocumentTextExtractionResult(true, Truncate(text), null);
    }

    private static Task<DocumentTextExtractionResult> ExtractPdfAsync(Stream content, CancellationToken cancellationToken)
    {
        try
        {
            using var document = PdfDocument.Open(content);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(page.Text);
            }

            var text = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(new DocumentTextExtractionResult(
                    false,
                    string.Empty,
                    "No extractable text found in PDF (it may be scanned/image-only)."));
            }

            return Task.FromResult(new DocumentTextExtractionResult(true, Truncate(text), null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new DocumentTextExtractionResult(
                false,
                string.Empty,
                $"Failed to read PDF: {ex.Message}"));
        }
    }

    private static string Truncate(string text) =>
        text.Length <= MaxChars ? text : text[..MaxChars];
}

public sealed class PolicyUrlFetcher : IPolicyUrlFetcher
{
    private const int MaxBytes = 2_000_000;
    private const int MaxChars = 50_000;

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "0.0.0.0",
        "::1"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PolicyUrlFetcher> _logger;

    public PolicyUrlFetcher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PolicyUrlFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PolicyUrlFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return new PolicyUrlFetchResult(false, string.Empty, null, "URL must be a valid http or https address.");
        }

        if (BlockedHosts.Contains(uri.Host) || IsPrivateIp(uri))
        {
            return new PolicyUrlFetchResult(false, string.Empty, null, "URL host is not allowed.");
        }

        var allowlist = _configuration.GetSection("PolicyIntake:AllowedHosts").Get<string[]>() ?? [];
        if (allowlist.Length > 0
            && !allowlist.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)
                                   || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase)))
        {
            return new PolicyUrlFetchResult(false, string.Empty, null, "URL host is not in the allowlist.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("PolicyUrlFetcher");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PolicyUrlFetchResult(
                    false,
                    string.Empty,
                    null,
                    $"URL returned HTTP {(int)response.StatusCode}.");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!mediaType.Contains("text", StringComparison.OrdinalIgnoreCase)
                && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
                && !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return new PolicyUrlFetchResult(
                    false,
                    string.Empty,
                    null,
                    $"Unsupported content type: {mediaType}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            long total = 0;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxBytes)
                {
                    return new PolicyUrlFetchResult(false, string.Empty, null, "URL content exceeds size limit.");
                }

                ms.Write(buffer, 0, read);
            }

            var raw = Encoding.UTF8.GetString(ms.ToArray());
            var text = mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
                ? StripHtml(raw)
                : raw;

            if (string.IsNullOrWhiteSpace(text))
            {
                return new PolicyUrlFetchResult(false, string.Empty, null, "No readable text at URL.");
            }

            return new PolicyUrlFetchResult(
                true,
                text.Length <= MaxChars ? text : text[..MaxChars],
                uri.Host,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Policy URL fetch failed for {Url}", uri);
            return new PolicyUrlFetchResult(false, string.Empty, null, "Failed to fetch URL content.");
        }
    }

    private static bool IsPrivateIp(Uri uri)
    {
        if (!IPAddress.TryParse(uri.Host, out var ip))
        {
            return false;
        }

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168)
                   || bytes[0] == 127;
        }

        return false;
    }

    private static string StripHtml(string html)
    {
        var withoutScripts = Regex.Replace(html, "<script[^>]*>.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var text = TagRegex.Replace(withoutScripts, " ");
        return Regex.Replace(WebUtility.HtmlDecode(text), "\\s+", " ").Trim();
    }
}
