namespace EnhancementHub.Application.Abstractions;

public interface IAuditExportTokenService
{
    string CreateToken(string storagePath, DateTime expiresAtUtc);

    bool TryValidateToken(string token, out string storagePath);
}
