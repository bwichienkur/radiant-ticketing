using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class EnhancementRequestCustomFieldValue : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public Guid CustomFieldDefinitionId { get; set; }
    public string? TextValue { get; set; }
    public double? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public Guid? UserValueId { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public CustomFieldDefinition Definition { get; set; } = null!;
    public User? UserValue { get; set; }
}
