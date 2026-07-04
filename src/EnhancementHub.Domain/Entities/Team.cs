using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Application> OwnedApplications { get; set; } = new List<Application>();
    public ICollection<EnhancementRequest> EnhancementRequests { get; set; } = new List<EnhancementRequest>();
}
