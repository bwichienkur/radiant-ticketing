using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class CodeEntityProperty : BaseEntity
{
    public Guid CodeEntityMappingId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string ClrType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }

    public CodeEntityMapping Mapping { get; set; } = null!;
}
