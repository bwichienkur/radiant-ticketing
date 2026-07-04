using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed record SchemaDriftFindingModel(
    string Category,
    string Title,
    string Description,
    DriftSeverity Severity,
    string? EntityName = null,
    string? TableName = null,
    string? ColumnName = null);

public sealed record SchemaDriftReportModel(
    IReadOnlyList<SchemaDriftFindingModel> Findings);
