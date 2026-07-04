using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class Application : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? BusinessDomain { get; set; }
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public Guid OwnerTeamId { get; set; }
    public string? RiskSensitiveAreas { get; set; }

    public Team OwnerTeam { get; set; } = null!;
    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<ApplicationProfile> Profiles { get; set; } = new List<ApplicationProfile>();
    public ICollection<EnhancementRequest> TargetedRequests { get; set; } = new List<EnhancementRequest>();
    public ICollection<AffectedApplication> AnalysisImpacts { get; set; } = new List<AffectedApplication>();
}
