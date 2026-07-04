using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class ApplicationProfile : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid RepositoryId { get; set; }
    public string? Purpose { get; set; }
    public string? BusinessDomain { get; set; }
    public string? KeyComponents { get; set; }
    public string? DatabaseUsage { get; set; }
    public string? ExternalIntegrations { get; set; }
    public string? InternalDependencies { get; set; }
    public string? DeploymentNotes { get; set; }
    public string? RiskSensitiveAreas { get; set; }
    public string? OwnershipMetadata { get; set; }
    public DateTime GeneratedAt { get; set; }

    public Application Application { get; set; } = null!;
    public Repository Repository { get; set; } = null!;
}
