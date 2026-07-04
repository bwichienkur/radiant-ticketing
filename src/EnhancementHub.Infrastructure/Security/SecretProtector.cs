using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Security;

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedData);
}

public sealed class SecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;
    private readonly ILogger<SecretProtector> _logger;

    public SecretProtector(IDataProtectionProvider dataProtectionProvider, ILogger<SecretProtector> logger)
    {
        _protector = dataProtectionProvider.CreateProtector("EnhancementHub.Secrets.v1");
        _logger = logger;
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedData)
    {
        if (string.IsNullOrEmpty(protectedData))
        {
            return protectedData;
        }

        try
        {
            return _protector.Unprotect(protectedData);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Failed to unprotect secret data");
            throw;
        }
    }
}

/// <summary>
/// Development fallback when ASP.NET Data Protection is not configured.
/// </summary>
public sealed class DevelopmentSecretProtector : ISecretProtector
{
    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
    }

    public string Unprotect(string protectedData)
    {
        if (string.IsNullOrEmpty(protectedData))
        {
            return protectedData;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(protectedData));
    }
}
