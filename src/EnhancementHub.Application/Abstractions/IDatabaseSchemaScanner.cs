using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IDatabaseSchemaScanner
{
    Task<DatabaseSchemaScanResult> ScanAsync(
        string connectionString,
        DatabaseProviderType provider,
        CancellationToken cancellationToken = default);
}
