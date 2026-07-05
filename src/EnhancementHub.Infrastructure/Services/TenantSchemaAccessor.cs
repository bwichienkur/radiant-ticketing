namespace EnhancementHub.Infrastructure.Services;

public sealed class TenantSchemaAccessor : Application.Abstractions.ITenantSchemaAccessor
{
    public string? ActiveSchemaName { get; set; }
}
