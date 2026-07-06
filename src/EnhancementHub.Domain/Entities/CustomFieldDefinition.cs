using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class CustomFieldDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? OptionsJson { get; set; }
    public Guid? TenantId { get; set; }

    public ICollection<EnhancementRequestCustomFieldValue> Values { get; set; } =
        new List<EnhancementRequestCustomFieldValue>();
}
