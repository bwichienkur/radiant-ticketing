using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class EnhancementAnalysis : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public string? FeatureSummary { get; set; }
    public string? BusinessRequirement { get; set; }
    public string? TechnicalRequirements { get; set; }
    public double ConfidenceScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string? RiskExplanation { get; set; }
    public string? TestingPlan { get; set; }
    public string? RolloutPlan { get; set; }
    public string? RollbackPlan { get; set; }
    public string? OpenQuestions { get; set; }
    public string? ApprovalChecklist { get; set; }
    public string? FeatureCategory { get; set; }
    public string? BusinessGoal { get; set; }
    public bool NeedsClarification { get; set; }
    public string? AmbiguityNotes { get; set; }
    public bool IsApprovedSnapshot { get; set; }
    public int Version { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public ICollection<AnalysisFinding> Findings { get; set; } = new List<AnalysisFinding>();
    public ICollection<AffectedApplication> AffectedApplications { get; set; } = new List<AffectedApplication>();
    public ICollection<AffectedRepository> AffectedRepositories { get; set; } = new List<AffectedRepository>();
    public ICollection<AffectedComponent> AffectedComponents { get; set; } = new List<AffectedComponent>();
    public ICollection<DatabaseChangeRecommendation> DatabaseChangeRecommendations { get; set; } = new List<DatabaseChangeRecommendation>();
    public ICollection<ApiChangeRecommendation> ApiChangeRecommendations { get; set; } = new List<ApiChangeRecommendation>();
    public ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();
    public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
    public ICollection<AiPromptRun> AiPromptRuns { get; set; } = new List<AiPromptRun>();
}
