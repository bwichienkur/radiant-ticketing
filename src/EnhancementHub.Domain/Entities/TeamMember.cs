using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
