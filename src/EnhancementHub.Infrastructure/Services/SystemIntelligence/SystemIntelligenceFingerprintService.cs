using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SystemIntelligenceFingerprintService : ISystemIntelligenceFingerprintService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public SystemIntelligenceFingerprintService(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<string> ComputeApplicationFingerprintAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var repoTimestamps = await _dbContext.Repositories
            .AsNoTracking()
            .Where(r => r.ApplicationId == applicationId)
            .Select(r => r.LastIndexedAt ?? r.UpdatedAt)
            .ToListAsync(cancellationToken);

        var scanTimestamps = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .Where(c => c.ApplicationId == applicationId)
            .Select(c => c.LastScannedAt ?? c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var graphUpdated = await _dbContext.SystemGraphNodes
            .AsNoTracking()
            .Where(n => n.ApplicationId == applicationId)
            .MaxAsync(n => (DateTime?)n.LastUpdatedAt, cancellationToken);

        return BuildFingerprint(repoTimestamps, scanTimestamps, graphUpdated);
    }

    public async Task<bool> IsDriftScanStaleAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection?.LastDriftScanAt is null)
        {
            return true;
        }

        var lastRepoIndexed = await _dbContext.Repositories
            .AsNoTracking()
            .Where(r => r.ApplicationId == connection.ApplicationId)
            .MaxAsync(r => r.LastIndexedAt, cancellationToken);

        var sourceChangedAt = MaxTimestamp(connection.LastScannedAt, lastRepoIndexed);
        return connection.LastDriftScanAt < sourceChangedAt;
    }

    internal static string BuildFingerprint(
        IReadOnlyList<DateTime> repoTimestamps,
        IReadOnlyList<DateTime> scanTimestamps,
        DateTime? graphUpdated)
    {
        var sb = new StringBuilder();
        sb.Append("repos:");
        foreach (var timestamp in repoTimestamps.OrderBy(t => t))
        {
            sb.Append(timestamp.Ticks.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        sb.Append(";scans:");
        foreach (var timestamp in scanTimestamps.OrderBy(t => t))
        {
            sb.Append(timestamp.Ticks.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        sb.Append(";graph:").Append(graphUpdated?.Ticks.ToString(CultureInfo.InvariantCulture) ?? "0");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }

    private static DateTime MaxTimestamp(params DateTime?[] values)
    {
        var max = DateTime.MinValue;
        foreach (var value in values)
        {
            if (value.HasValue && value.Value > max)
            {
                max = value.Value;
            }
        }

        return max;
    }
}
