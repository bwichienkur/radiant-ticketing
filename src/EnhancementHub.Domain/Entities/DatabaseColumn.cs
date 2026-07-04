using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DatabaseColumn : BaseEntity
{
    public Guid DatabaseTableId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public int OrdinalPosition { get; set; }

    public DatabaseTable DatabaseTable { get; set; } = null!;
}
