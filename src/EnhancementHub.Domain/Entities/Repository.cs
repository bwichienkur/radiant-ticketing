using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class Repository : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ExternalTicketProvider Provider { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public string? GitTokenSecretName { get; set; }
    public string? SourceSubdirectory { get; set; }
    public int IndexingPriority { get; set; }
    public bool AutoIndexOnPush { get; set; } = true;
    public DateTime? LastIndexedAt { get; set; }
    public IndexingStatus IndexingStatus { get; set; }

    public Application Application { get; set; } = null!;
    public ICollection<RepositoryBranch> Branches { get; set; } = new List<RepositoryBranch>();
    public ICollection<IndexedFile> IndexedFiles { get; set; } = new List<IndexedFile>();
    public ICollection<ApplicationProfile> Profiles { get; set; } = new List<ApplicationProfile>();
    public ICollection<AffectedRepository> AnalysisImpacts { get; set; } = new List<AffectedRepository>();
    public ICollection<CodeEntityMapping> EntityMappings { get; set; } = new List<CodeEntityMapping>();
    public ICollection<CodeTableReference> TableReferences { get; set; } = new List<CodeTableReference>();
    public ICollection<SchemaDriftFinding> DriftFindings { get; set; } = new List<SchemaDriftFinding>();
    public ICollection<SystemGraphNode> GraphNodes { get; set; } = new List<SystemGraphNode>();
    public ICollection<RefactorPlan> RefactorPlans { get; set; } = new List<RefactorPlan>();
}
