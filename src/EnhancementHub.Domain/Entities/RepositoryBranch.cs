using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class RepositoryBranch : BaseEntity
{
    public Guid RepositoryId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? LastCommitHash { get; set; }
    public DateTime? LastIndexedAt { get; set; }

    public Repository Repository { get; set; } = null!;
    public ICollection<IndexedFile> IndexedFiles { get; set; } = new List<IndexedFile>();
}
