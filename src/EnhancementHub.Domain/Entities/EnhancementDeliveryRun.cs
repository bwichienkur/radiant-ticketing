using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class EnhancementDeliveryRun : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }

    public int RunNumber { get; set; } = 1;

    public DeliveryRunPhase Phase { get; set; } = DeliveryRunPhase.Pending;

    public bool IsSimulation { get; set; }

    public string? BranchName { get; set; }

    public string? PullRequestUrl { get; set; }

    public int? PullRequestNumber { get; set; }

    public string? CommitSha { get; set; }

    public string? TestUrl { get; set; }

    public string? TestDeployReference { get; set; }

    public string? QaStepsJson { get; set; }

    public bool? QaPassed { get; set; }

    public string? QaVideoStoragePath { get; set; }

    public string? QaReportStoragePath { get; set; }

    public QaRunnerKind QaRunner { get; set; } = QaRunnerKind.Simulated;

    public DateTime? QaStartedAt { get; set; }

    public DateTime? QaFinishedAt { get; set; }

    public Guid? UatSignedOffByUserId { get; set; }

    public DateTime? UatSignedOffAt { get; set; }

    public string? UatNotes { get; set; }

    public bool UatApproved { get; set; }

    public DateTime? ProdScheduledAt { get; set; }

    public string? ProdDeployReference { get; set; }

    public DateTime? ProdDeployedAt { get; set; }

    public string? ProdArtifactReference { get; set; }

    public string? RollbackTargetDeployReference { get; set; }

    public string? RollbackTargetCommitSha { get; set; }

    public DateTime? RolledBackAt { get; set; }

    public bool? PostDeploySmokePassed { get; set; }

    public string? TimelineJson { get; set; }

    public string? LastError { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;

    public ICollection<DeliveryRunTestResult> TestResults { get; set; } = new List<DeliveryRunTestResult>();
}
