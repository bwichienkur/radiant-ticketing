using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Domain.Entities;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Common.Mappings;

public static class MappingExtensions
{
    public static EnhancementRequestDto ToDto(this EnhancementRequest entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.BusinessDescription,
            entity.DesiredOutcome,
            entity.Priority,
            entity.TargetApplicationId,
            entity.TargetApplication?.Name,
            entity.RequestedDueDate,
            entity.SubmittedByUserId,
            entity.SubmittedByUser?.DisplayName,
            entity.Department,
            entity.TeamId,
            entity.Status,
            entity.SupportingNotes,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static EnhancementRequestDetailDto ToDetailDto(this EnhancementRequest entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.BusinessDescription,
            entity.DesiredOutcome,
            entity.Priority,
            entity.TargetApplicationId,
            entity.TargetApplication?.Name,
            entity.RequestedDueDate,
            entity.SubmittedByUserId,
            entity.SubmittedByUser?.DisplayName ?? string.Empty,
            entity.Department,
            entity.TeamId,
            entity.Status,
            entity.SupportingNotes,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Attachments.Select(a => new EnhancementAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.CreatedAt)).ToList(),
            entity.Comments.Select(c => new CommentSummaryDto(
                c.Id,
                c.UserId,
                c.User?.DisplayName ?? string.Empty,
                c.Content,
                c.IsInternal,
                c.CreatedAt)).ToList(),
            entity.Analyses
                .OrderByDescending(a => a.Version)
                .Select(a => a.ToSummaryDto())
                .ToList());

    public static EnhancementAnalysisSummaryDto ToSummaryDto(this EnhancementAnalysis entity) =>
        new(
            entity.Id,
            entity.Version,
            entity.FeatureSummary,
            entity.RiskLevel,
            entity.ConfidenceScore,
            entity.NeedsClarification,
            entity.IsApprovedSnapshot,
            entity.CreatedAt);

    public static EnhancementAnalysisDto ToDto(this EnhancementAnalysis entity) =>
        new(
            entity.Id,
            entity.EnhancementRequestId,
            entity.Version,
            entity.FeatureSummary,
            entity.BusinessRequirement,
            entity.TechnicalRequirements,
            entity.ConfidenceScore,
            entity.RiskLevel,
            entity.RiskExplanation,
            entity.TestingPlan,
            entity.RolloutPlan,
            entity.RollbackPlan,
            entity.OpenQuestions,
            entity.ApprovalChecklist,
            entity.FeatureCategory,
            entity.BusinessGoal,
            entity.NeedsClarification,
            entity.AmbiguityNotes,
            entity.IsApprovedSnapshot,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Findings.Select(f => f.ToDto()).ToList(),
            entity.AffectedApplications.Select(a => a.ToDto()).ToList(),
            entity.AffectedRepositories.Select(r => r.ToDto()).ToList(),
            entity.AffectedComponents.Select(c => c.ToDto()).ToList(),
            entity.DatabaseChangeRecommendations.Select(d => d.ToDto()).ToList(),
            entity.ApiChangeRecommendations.Select(a => a.ToDto()).ToList(),
            entity.RiskAssessments.Select(r => r.ToDto()).ToList());

    public static AnalysisFindingDto ToDto(this AnalysisFinding entity) =>
        new(
            entity.Id,
            entity.Category,
            entity.Title,
            entity.Description,
            entity.ConfidenceScore,
            entity.IsAiSuggested,
            entity.IsHumanApproved);

    public static AffectedApplicationDto ToDto(this AffectedApplication entity) =>
        new(
            entity.Id,
            entity.ApplicationId,
            entity.Application?.Name,
            entity.ImpactDescription,
            entity.ConfidenceScore);

    public static AffectedRepositoryDto ToDto(this AffectedRepository entity) =>
        new(
            entity.Id,
            entity.RepositoryId,
            entity.Repository?.Name,
            entity.ImpactDescription,
            entity.ConfidenceScore);

    public static AffectedComponentDto ToDto(this AffectedComponent entity) =>
        new(
            entity.Id,
            entity.IndexedFileId,
            entity.ComponentPath,
            entity.ComponentType,
            entity.ImpactDescription,
            entity.ChangeType,
            entity.ConfidenceScore);

    public static DatabaseChangeRecommendationDto ToDto(this DatabaseChangeRecommendation entity) =>
        new(
            entity.Id,
            entity.TableName,
            entity.ChangeType,
            entity.Description,
            entity.MigrationRequired,
            entity.ConfidenceScore,
            entity.IsAiSuggested);

    public static ApiChangeRecommendationDto ToDto(this ApiChangeRecommendation entity) =>
        new(
            entity.Id,
            entity.Endpoint,
            entity.HttpMethod,
            entity.ChangeType,
            entity.Description,
            entity.ConfidenceScore,
            entity.IsAiSuggested);

    public static RiskAssessmentDto ToDto(this RiskAssessment entity) =>
        new(
            entity.Id,
            entity.RiskLevel,
            entity.SecurityConcerns,
            entity.PerformanceConcerns,
            entity.Explanation,
            entity.ConfidenceScore);

    public static RepositoryDto ToDto(this Repository entity) =>
        new(
            entity.Id,
            entity.ApplicationId,
            entity.Application?.Name,
            entity.Name,
            entity.Url,
            entity.Provider,
            entity.DefaultBranch,
            entity.LastIndexedAt,
            entity.IndexingStatus);

    public static ApplicationDto ToDto(this ApplicationEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.BusinessDomain,
            entity.Purpose,
            entity.Description,
            entity.OwnerTeamId,
            entity.RiskSensitiveAreas,
            entity.Repositories.Count);

    public static ApplicationProfileDto ToDto(this ApplicationProfile entity) =>
        new(
            entity.Id,
            entity.ApplicationId,
            entity.RepositoryId,
            entity.Purpose,
            entity.BusinessDomain,
            entity.KeyComponents,
            entity.DatabaseUsage,
            entity.ExternalIntegrations,
            entity.InternalDependencies,
            entity.DeploymentNotes,
            entity.RiskSensitiveAreas,
            entity.OwnershipMetadata,
            entity.GeneratedAt);

    public static ApprovalActionDto ToDto(this ApprovalAction entity) =>
        new(
            entity.Id,
            entity.EnhancementRequestId,
            entity.EnhancementAnalysisId,
            entity.UserId,
            entity.User?.DisplayName ?? string.Empty,
            entity.ActionType,
            entity.Comments,
            entity.PreviousValue,
            entity.NewValue,
            entity.CreatedAt);
}
