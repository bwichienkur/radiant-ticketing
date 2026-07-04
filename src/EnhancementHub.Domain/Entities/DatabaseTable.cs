using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DatabaseTable : BaseEntity
{
    public Guid DatabaseConnectionId { get; set; }
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public long? RowCountEstimate { get; set; }
    public string? Description { get; set; }
    public DateTime CapturedAt { get; set; }

    public DatabaseConnection DatabaseConnection { get; set; } = null!;
    public ICollection<DatabaseColumn> Columns { get; set; } = new List<DatabaseColumn>();
    public ICollection<DatabaseRelationship> OutgoingRelationships { get; set; } = new List<DatabaseRelationship>();
    public ICollection<DatabaseRelationship> IncomingRelationships { get; set; } = new List<DatabaseRelationship>();
}
