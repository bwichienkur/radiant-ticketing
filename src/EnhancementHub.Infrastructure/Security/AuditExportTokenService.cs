using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Security;

public sealed class AuditExportTokenService : IAuditExportTokenService
{
    private readonly byte[] _signingKey;

    public AuditExportTokenService(IConfiguration configuration)
    {
        var secret = configuration["AuditExport:SigningKey"]
            ?? configuration["Jwt:Secret"]
            ?? "dev-secret-change-in-production-min-32-chars!!";
        _signingKey = Encoding.UTF8.GetBytes(secret);
    }

    public string CreateToken(string storagePath, DateTime expiresAtUtc)
    {
        var payload = new AuditExportTokenPayload(storagePath, expiresAtUtc);
        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadSegment = Base64UrlEncode(payloadBytes);
        var signature = ComputeSignature(payloadSegment);
        return $"{payloadSegment}.{signature}";
    }

    public bool TryValidateToken(string token, out string storagePath)
    {
        storagePath = string.Empty;
        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var expectedSignature = ComputeSignature(parts[0]);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(parts[1])))
        {
            return false;
        }

        AuditExportTokenPayload? payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            payload = JsonSerializer.Deserialize<AuditExportTokenPayload>(json);
        }
        catch
        {
            return false;
        }

        if (payload is null || payload.ExpiresAtUtc < DateTime.UtcNow)
        {
            return false;
        }

        storagePath = payload.StoragePath;
        return !string.IsNullOrWhiteSpace(storagePath);
    }

    private string ComputeSignature(string payloadSegment)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }

    private sealed record AuditExportTokenPayload(string StoragePath, DateTime ExpiresAtUtc);
}
