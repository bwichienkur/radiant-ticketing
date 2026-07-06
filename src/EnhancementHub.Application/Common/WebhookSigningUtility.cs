using System.Security.Cryptography;
using System.Text;

namespace EnhancementHub.Application.Common;

public static class WebhookSigningUtility
{
    public const string SignatureHeaderName = "X-EnhancementHub-Signature";

    public static string CreateSignatureHeader(string payload, string secret, DateTimeOffset? timestamp = null)
    {
        var unixTimestamp = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var signedPayload = $"{unixTimestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={unixTimestamp},v1={signature}";
    }

    public static bool VerifySignature(string payload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var timestamp = ExtractSignaturePart(signatureHeader, "t");
        var signature = ExtractSignaturePart(signatureHeader, "v1");
        if (timestamp is null || signature is null)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private static string? ExtractSignaturePart(string header, string key)
    {
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (string.Equals(part[..separator], key, StringComparison.OrdinalIgnoreCase))
            {
                return part[(separator + 1)..];
            }
        }

        return null;
    }
}
