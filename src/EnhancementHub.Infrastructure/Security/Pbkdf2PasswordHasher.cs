using System.Security.Cryptography;
using System.Text;
using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace EnhancementHub.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int DefaultIterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DeriveKey(password, salt, DefaultIterations);
        return $"{DefaultIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.', 3);
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var iterations)
            || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = DeriveKey(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations) =>
        KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            iterations,
            KeySize);
}
