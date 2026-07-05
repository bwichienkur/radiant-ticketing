using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class EnhancementTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DomainCategory { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string? SupportingNotes { get; set; }
    public bool IsActive { get; set; } = true;
}
