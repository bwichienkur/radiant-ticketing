using MediatR;

namespace EnhancementHub.Application.AuditLogs;

public sealed record ExportAuditLogsQuery(
    string Format,
    string? EntityType = null,
    string? Action = null,
    Guid? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Limit = 10000) : IRequest<AuditLogExportResult>;
