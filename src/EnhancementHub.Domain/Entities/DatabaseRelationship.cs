using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DatabaseRelationship : BaseEntity
{
    public Guid DatabaseConnectionId { get; set; }
    public Guid FromTableId { get; set; }
    public string FromColumnName { get; set; } = string.Empty;
    public Guid ToTableId { get; set; }
    public string ToColumnName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = "OneToMany";

    public DatabaseConnection DatabaseConnection { get; set; } = null!;
    public DatabaseTable FromTable { get; set; } = null!;
    public DatabaseTable ToTable { get; set; } = null!;
}
