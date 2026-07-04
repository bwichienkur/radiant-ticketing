using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class SchemaDriftReport : BaseEntity
{
    public Guid ConnectionId { get; set; }
    public Guid? RepositoryId { get; set; }
    public DateTime DetectedAt { get; set; }

    public DatabaseConnection Connection { get; set; } = null!;
    public Repository? Repository { get; set; }
}
