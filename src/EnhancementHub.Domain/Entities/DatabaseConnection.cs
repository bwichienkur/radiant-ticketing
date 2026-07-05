using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class DatabaseConnection : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DatabaseProviderType Provider { get; set; }
    public string ConnectionStringProtected { get; set; } = string.Empty;
    public string? Host { get; set; }
    public string? DatabaseName { get; set; }
    public bool IsReadOnly { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public DateTime? LastDriftScanAt { get; set; }
    public string ScanStatus { get; set; } = "Pending";
    public string? ScanError { get; set; }

    public Application Application { get; set; } = null!;
    public ICollection<DatabaseTable> Tables { get; set; } = new List<DatabaseTable>();
    public ICollection<DatabaseRelationship> Relationships { get; set; } = new List<DatabaseRelationship>();
    public ICollection<SchemaDriftFinding> DriftFindings { get; set; } = new List<SchemaDriftFinding>();
    public ICollection<RefactorPlan> RefactorPlans { get; set; } = new List<RefactorPlan>();
}
