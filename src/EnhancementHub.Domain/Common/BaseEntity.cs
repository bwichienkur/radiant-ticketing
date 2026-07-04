using EnhancementHub.Domain.Abstractions;

namespace EnhancementHub.Domain.Common;

public abstract class BaseEntity : IAuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
