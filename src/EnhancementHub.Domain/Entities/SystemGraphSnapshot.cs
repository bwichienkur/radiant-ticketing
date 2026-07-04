using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class SystemGraphSnapshot : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string GraphJson { get; set; } = "{}";
    public DateTime BuiltAt { get; set; }

    public Application Application { get; set; } = null!;
}
