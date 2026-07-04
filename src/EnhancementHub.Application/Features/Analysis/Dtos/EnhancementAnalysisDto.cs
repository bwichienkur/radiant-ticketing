using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Analysis.Dtos;

public sealed record EnhancementAnalysisDto(
    Guid Id,
    Guid EnhancementRequestId,
    int Version,
    string? FeatureSummary,
    string? BusinessRequirement,
    string? TechnicalRequirements,
    double ConfidenceScore,
    RiskLevel RiskLevel,
    string? RiskExplanation,
    string? TestingPlan,
    string? RolloutPlan,
    string? RollbackPlan,
    string? OpenQuestions,
    string? ApprovalChecklist,
    string? FeatureCategory,
    string? BusinessGoal,
    bool NeedsClarification,
    string? AmbiguityNotes,
    bool IsApprovedSnapshot,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AnalysisFindingDto> Findings,
    IReadOnlyList<AffectedApplicationDto> AffectedApplications,
    IReadOnlyList<AffectedRepositoryDto> AffectedRepositories,
    IReadOnlyList<AffectedComponentDto> AffectedComponents,
    IReadOnlyList<DatabaseChangeRecommendationDto> DatabaseChangeRecommendations,
    IReadOnlyList<ApiChangeRecommendationDto> ApiChangeRecommendations,
    IReadOnlyList<RiskAssessmentDto> RiskAssessments);

public sealed record AnalysisFindingDto(
    Guid Id,
    string Category,
    string Title,
    string Description,
    double ConfidenceScore,
    bool IsAiSuggested,
    bool IsHumanApproved);

public sealed record AffectedApplicationDto(
    Guid Id,
    Guid ApplicationId,
    string? ApplicationName,
    string ImpactDescription,
    double ConfidenceScore);

public sealed record AffectedRepositoryDto(
    Guid Id,
    Guid RepositoryId,
    string? RepositoryName,
    string ImpactDescription,
    double ConfidenceScore);

public sealed record AffectedComponentDto(
    Guid Id,
    Guid? IndexedFileId,
    string ComponentPath,
    ComponentType ComponentType,
    string ImpactDescription,
    string ChangeType,
    double ConfidenceScore);

public sealed record DatabaseChangeRecommendationDto(
    Guid Id,
    string TableName,
    string ChangeType,
    string Description,
    bool MigrationRequired,
    double ConfidenceScore,
    bool IsAiSuggested);

public sealed record ApiChangeRecommendationDto(
    Guid Id,
    string Endpoint,
    string HttpMethod,
    string ChangeType,
    string Description,
    double ConfidenceScore,
    bool IsAiSuggested);

public sealed record RiskAssessmentDto(
    Guid Id,
    RiskLevel RiskLevel,
    string? SecurityConcerns,
    string? PerformanceConcerns,
    string? Explanation,
    double ConfidenceScore);
