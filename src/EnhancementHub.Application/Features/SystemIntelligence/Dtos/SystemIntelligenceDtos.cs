using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.SystemIntelligence.Dtos;

public sealed record DatabaseConnectionDto(
    Guid Id,
    Guid ApplicationId,
    string? ApplicationName,
    string Name,
    DatabaseProviderType Provider,
    bool IsReadOnly,
    string ScanStatus,
    DateTime? LastScannedAt,
    string? ScanError);

public sealed record DatabaseColumnDto(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsForeignKey,
    int OrdinalPosition);

public sealed record DatabaseTableDto(
    Guid Id,
    string SchemaName,
    string TableName,
    IReadOnlyList<DatabaseColumnDto> Columns);

public sealed record DatabaseRelationshipDto(
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn,
    string RelationshipType);

public sealed record DatabaseSchemaDto(
    Guid ConnectionId,
    string ConnectionName,
    IReadOnlyList<DatabaseTableDto> Tables,
    IReadOnlyList<DatabaseRelationshipDto> Relationships);

public sealed record SystemGraphNodeDto(
    string Id,
    string Label,
    string Type,
    string? Detail);

public sealed record SystemGraphEdgeDto(
    string FromId,
    string ToId,
    string Label);

public sealed record SystemMapDto(
    Guid ApplicationId,
    string? ApplicationName,
    IReadOnlyList<SystemGraphNodeDto> Nodes,
    IReadOnlyList<SystemGraphEdgeDto> Edges,
    DateTime? BuiltAt);

public sealed record ErdDiagramDto(
    Guid ApplicationId,
    string Mermaid);

public sealed record SchemaDriftFindingDto(
    Guid Id,
    DriftType DriftType,
    DriftSeverity Severity,
    string Title,
    string Description,
    string? CodeReference,
    string? DatabaseReference,
    DateTime DetectedAt,
    bool IsResolved);

public sealed record DriftReportDto(
    Guid ConnectionId,
    Guid? RepositoryId,
    DateTime? DetectedAt,
    IReadOnlyList<SchemaDriftFindingDto> Findings);

public sealed record BlastRadiusItemDto(
    string Name,
    string Type,
    string Impact,
    int Depth);

public sealed record BlastRadiusResultDto(
    string TargetName,
    IReadOnlyList<BlastRadiusItemDto> AffectedItems);

public sealed record RefactorPlanDto(
    Guid Id,
    string Title,
    string TargetDescription,
    RefactorPlanStatus Status,
    RiskLevel RiskLevel,
    double ConfidenceScore,
    Guid? DatabaseConnectionId,
    Guid? RepositoryId,
    DateTime CreatedAt);

public sealed record RefactorPlanDetailDto(
    Guid Id,
    string Title,
    string TargetDescription,
    string? PlanMarkdown,
    BlastRadiusResultDto? BlastRadius,
    RefactorPlanStatus Status,
    DateTime CreatedAt);

public sealed record DocumentationExportResultDto(
    Guid ApplicationId,
    string Content,
    string ContentType,
    string FileName);

public sealed record OnPremAgentDto(
    Guid Id,
    string Name,
    Guid? ApplicationId,
    DateTime? LastSeenAt,
    bool IsActive,
    string? ApiKey);
