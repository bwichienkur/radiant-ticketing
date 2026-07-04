using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IDatabaseSchemaIngestionService
{
    Task IngestAsync(Guid connectionId, CancellationToken cancellationToken = default);

    Task IngestScanResultAsync(
        Guid connectionId,
        DatabaseSchemaScanResult scanResult,
        CancellationToken cancellationToken = default);
}
