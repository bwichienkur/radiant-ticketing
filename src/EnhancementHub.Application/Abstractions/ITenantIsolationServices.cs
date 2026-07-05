using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface ITenantSchemaAccessor
{
    string? ActiveSchemaName { get; set; }
}

public interface ITenantIsolationService
{
    Task<TenantIsolationStatus> GetIsolationStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantIsolationStatus> ProvisionDedicatedSchemaAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task TryAutoProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantIsolationStatus(
    Guid TenantId,
    string IsolationMode,
    string? DatabaseSchemaName,
    bool IsSchemaProvisioned,
    DateTime? SchemaProvisionedAt,
    bool IsolationEnabled);
