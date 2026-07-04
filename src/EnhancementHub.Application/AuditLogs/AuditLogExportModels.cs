namespace EnhancementHub.Application.AuditLogs;

public sealed record AuditLogExportRecord(
    Guid Id,
    DateTime CreatedAt,
    string Action,
    string EntityType,
    Guid EntityId,
    Guid? UserId,
    string? UserDisplayName,
    string? Comments,
    string? PreviousValue,
    string? NewValue,
    Guid? CorrelationId);

public sealed record AuditLogExportResult(
    byte[] Content,
    string ContentType,
    string FileName);
