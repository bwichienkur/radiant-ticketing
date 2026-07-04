using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface ISchemaDriftDetector
{
    Task<DriftReport> DetectDriftAsync(Guid databaseConnectionId, CancellationToken cancellationToken = default);
}
