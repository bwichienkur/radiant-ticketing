using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public sealed record ExternalTicketExportRequest(
    Guid EnhancementRequestId,
    string Title,
    string Description,
    string Priority,
    string? AnalysisSummary);

public sealed record ExternalTicketExportResult(
    bool Success,
    string? ExternalId,
    string? Url,
    string? ErrorMessage);

public interface IExternalTicketExporter
{
    ExternalTicketProvider Provider { get; }
    Task<ExternalTicketExportResult> ExportAsync(ExternalTicketExportRequest request, CancellationToken cancellationToken = default);
}

public interface IExternalTicketExporterFactory
{
    IExternalTicketExporter GetExporter(ExternalTicketProvider provider);
}
