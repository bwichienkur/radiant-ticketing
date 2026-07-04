using System.Net.Sockets;
using System.Text;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

/// <summary>
/// Validates attachment type/size and optionally scans via ClamAV INSTREAM protocol.
/// </summary>
public sealed class AttachmentScanService : IAttachmentScanService
{
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".md", ".csv",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".json", ".xml"
    ];

    internal static readonly Dictionary<string, byte[][]> MagicSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        [".zip"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06]]
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<AttachmentScanService> _logger;

    public AttachmentScanService(IConfiguration configuration, ILogger<AttachmentScanService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AttachmentScanResult> ScanAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("Attachments:Scanning:Enabled", true))
        {
            return new AttachmentScanResult(true, "Skipped", "Attachment scanning is disabled.");
        }

        var maxBytes = _configuration.GetValue("Attachments:Scanning:MaxFileSizeBytes", 20_000_000L);
        if (content.CanSeek && content.Length > maxBytes)
        {
            return new AttachmentScanResult(false, "Rejected", $"File exceeds maximum size of {maxBytes} bytes.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            return new AttachmentScanResult(false, "Rejected", $"File type '{extension}' is not allowed.");
        }

        var buffer = new byte[Math.Min(8192, maxBytes)];
        var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        if (read == 0)
        {
            return new AttachmentScanResult(false, "Rejected", "File is empty.");
        }

        if (MagicSignatures.TryGetValue(extension, out var signatures)
            && !signatures.Any(sig => StartsWith(buffer, read, sig)))
        {
            return new AttachmentScanResult(false, "Rejected", "File content does not match its extension.");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        if (_configuration.GetValue("Attachments:Scanning:ClamAv:Enabled", false))
        {
            var clamResult = await ScanWithClamAvAsync(content, cancellationToken);
            if (!clamResult.IsAllowed)
            {
                return clamResult;
            }
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return new AttachmentScanResult(true, "Clean", "Attachment passed validation.");
    }

    private async Task<AttachmentScanResult> ScanWithClamAvAsync(Stream content, CancellationToken cancellationToken)
    {
        var host = _configuration["Attachments:Scanning:ClamAv:Host"] ?? "localhost";
        var port = _configuration.GetValue("Attachments:Scanning:ClamAv:Port", 3310);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();

            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);

            var chunk = new byte[8192];
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(chunk, cancellationToken)) > 0)
            {
                var sizeBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(bytesRead));
                await stream.WriteAsync(sizeBytes, cancellationToken);
                await stream.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
            }

            await stream.WriteAsync(BitConverter.GetBytes(0), cancellationToken);

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            var response = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;

            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ClamAV detected threat: {Response}", response);
                return new AttachmentScanResult(false, "Rejected", response);
            }

            return new AttachmentScanResult(true, "Clean", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClamAV scan unavailable");
            if (_configuration.GetValue("Attachments:Scanning:ClamAv:Required", false))
            {
                return new AttachmentScanResult(false, "Rejected", "Virus scanner is required but unavailable.");
            }

            return new AttachmentScanResult(true, "Clean", "ClamAV unavailable; basic validation only.");
        }
        finally
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }
        }
    }

    internal static bool StartsWith(byte[] buffer, int length, byte[] signature)
    {
        if (length < signature.Length)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}
