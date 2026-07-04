using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class IndexedFile : BaseEntity
{
    public Guid RepositoryId { get; set; }
    public Guid BranchId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Project { get; set; }
    public string? Language { get; set; }
    public string? FileType { get; set; }
    public ComponentType ComponentType { get; set; }
    public string? Namespace { get; set; }
    public string? ClassName { get; set; }
    public string? Summary { get; set; }
    public string? ExtractedDependencies { get; set; }
    public string? RelatedDatabaseObjects { get; set; }
    public string? RelatedApis { get; set; }
    public string? CommitHash { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public float[]? EmbeddingVector { get; set; }

    public Repository Repository { get; set; } = null!;
    public RepositoryBranch Branch { get; set; } = null!;
    public ICollection<IndexedSymbol> Symbols { get; set; } = new List<IndexedSymbol>();
    public ICollection<AffectedComponent> AnalysisImpacts { get; set; } = new List<AffectedComponent>();
    public ICollection<RetrievedContextItem> RetrievedContextItems { get; set; } = new List<RetrievedContextItem>();
}
