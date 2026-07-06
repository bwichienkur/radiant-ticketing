using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class Application : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? BusinessDomain { get; set; }
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public Guid OwnerTeamId { get; set; }
    public string? RiskSensitiveAreas { get; set; }
    public string? DeploymentNotes { get; set; }
    public ApplicationTier Tier { get; set; } = ApplicationTier.Standard;

    public Team OwnerTeam { get; set; } = null!;
    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<ApplicationProfile> Profiles { get; set; } = new List<ApplicationProfile>();
    public ICollection<EnhancementRequest> TargetedRequests { get; set; } = new List<EnhancementRequest>();
    public ICollection<AffectedApplication> AnalysisImpacts { get; set; } = new List<AffectedApplication>();
    public ICollection<DatabaseConnection> DatabaseConnections { get; set; } = new List<DatabaseConnection>();
    public ICollection<SystemGraphNode> GraphNodes { get; set; } = new List<SystemGraphNode>();
    public ICollection<SystemGraphSnapshot> SystemGraphSnapshots { get; set; } = new List<SystemGraphSnapshot>();
    public ICollection<DocumentationExportCache> DocumentationExportCaches { get; set; } = new List<DocumentationExportCache>();
    public ICollection<OpenApiRegistration> OpenApiRegistrations { get; set; } = new List<OpenApiRegistration>();
    public ICollection<OnPremAgent> OnPremAgents { get; set; } = new List<OnPremAgent>();
    public ApplicationDeliveryProfile? DeliveryProfile { get; set; }
    public ICollection<ApplicationTestSuite> TestSuites { get; set; } = new List<ApplicationTestSuite>();
}
