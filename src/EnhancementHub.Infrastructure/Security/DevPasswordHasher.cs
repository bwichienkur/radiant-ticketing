using System.Security.Cryptography;
using System.Text;
using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Security;

public sealed class DevPasswordHasher : IPasswordHasher
{
    private const string Salt = "EnhancementHub.Dev.Salt.v1";

    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Salt + password));
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string password, string hash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(password)),
            Encoding.UTF8.GetBytes(hash));
}
