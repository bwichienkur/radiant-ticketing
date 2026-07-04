using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class SchemaDriftFinding : BaseEntity
{
    public Guid DatabaseConnectionId { get; set; }
    public Guid? RepositoryId { get; set; }
    public DriftType DriftType { get; set; }
    public DriftSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CodeReference { get; set; }
    public string? DatabaseReference { get; set; }
    public bool IsResolved { get; set; }
    public DateTime DetectedAt { get; set; }

    public DatabaseConnection DatabaseConnection { get; set; } = null!;
    public Repository? Repository { get; set; }
}
