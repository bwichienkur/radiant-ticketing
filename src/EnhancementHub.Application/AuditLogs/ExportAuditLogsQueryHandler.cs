using System.Globalization;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.AuditLogs;

public sealed class ExportAuditLogsQueryHandler : IRequestHandler<ExportAuditLogsQuery, AuditLogExportResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IEnhancementHubDbContext _db;

    public ExportAuditLogsQueryHandler(IEnhancementHubDbContext db) => _db = db;

    public async Task<AuditLogExportResult> Handle(
        ExportAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var format = request.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ? "json" : "csv";
        var limit = Math.Clamp(request.Limit, 1, 50000);
        var records = await QueryRecordsAsync(request, limit, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        if (format == "json")
        {
            var json = JsonSerializer.Serialize(records, JsonOptions);
            return new AuditLogExportResult(
                Encoding.UTF8.GetBytes(json),
                "application/json",
                $"audit-log-{timestamp}.json");
        }

        return new AuditLogExportResult(
            Encoding.UTF8.GetBytes(BuildCsv(records)),
            "text/csv",
            $"audit-log-{timestamp}.csv");
    }

    private async Task<IReadOnlyList<AuditLogExportRecord>> QueryRecordsAsync(
        ExportAuditLogsQuery request,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId);
        }

        if (request.From.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= request.To.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogExportRecord(
                a.Id,
                a.CreatedAt,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.UserId,
                a.User != null ? a.User.DisplayName : null,
                a.Comments,
                a.PreviousValue,
                a.NewValue,
                a.CorrelationId))
            .ToListAsync(cancellationToken);
    }

    private static string BuildCsv(IReadOnlyList<AuditLogExportRecord> records)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id,CreatedAtUtc,Action,EntityType,EntityId,UserId,UserDisplayName,Comments,PreviousValue,NewValue,CorrelationId");

        foreach (var record in records)
        {
            builder.Append(EscapeCsv(record.Id.ToString())).Append(',');
            builder.Append(EscapeCsv(record.CreatedAt.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(EscapeCsv(record.Action)).Append(',');
            builder.Append(EscapeCsv(record.EntityType)).Append(',');
            builder.Append(EscapeCsv(record.EntityId.ToString())).Append(',');
            builder.Append(EscapeCsv(record.UserId?.ToString())).Append(',');
            builder.Append(EscapeCsv(record.UserDisplayName)).Append(',');
            builder.Append(EscapeCsv(record.Comments)).Append(',');
            builder.Append(EscapeCsv(record.PreviousValue)).Append(',');
            builder.Append(EscapeCsv(record.NewValue)).Append(',');
            builder.AppendLine(EscapeCsv(record.CorrelationId?.ToString()));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
