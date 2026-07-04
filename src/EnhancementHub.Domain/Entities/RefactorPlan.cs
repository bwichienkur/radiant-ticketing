using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class RefactorPlan : BaseEntity
{
    public Guid? EnhancementRequestId { get; set; }
    public Guid? DatabaseConnectionId { get; set; }
    public Guid? RepositoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TargetDescription { get; set; } = string.Empty;
    public string? BlastRadiusJson { get; set; }
    public string? MigrationStepsJson { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public double ConfidenceScore { get; set; }
    public RefactorPlanStatus Status { get; set; } = RefactorPlanStatus.Draft;
    public bool GeneratedByAi { get; set; }

    public EnhancementRequest? EnhancementRequest { get; set; }
    public DatabaseConnection? DatabaseConnection { get; set; }
    public Repository? Repository { get; set; }
}
