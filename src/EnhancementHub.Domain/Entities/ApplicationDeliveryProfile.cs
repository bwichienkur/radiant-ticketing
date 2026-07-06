using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

/// <summary>
/// Per-application deployment mechanism and environment-specific config (Phase A — config only).
/// </summary>
public class ApplicationDeliveryProfile : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public DeploymentMechanism DeploymentMechanism { get; set; } = DeploymentMechanism.AppService;

    public Guid? PrimaryRepositoryId { get; set; }

    public string BranchNamingPattern { get; set; } = "eh/{requestId}-{slug}";

    /// <summary>Workflow file path, pipeline ID, or webhook route.</summary>
    public string? CicdPipelineReference { get; set; }

    public CicdProvider? CicdProviderOverride { get; set; }

    public string SmokeTestPath { get; set; } = "/health";

    public DatabaseMigrationStrategy DatabaseMigrationStrategy { get; set; } = DatabaseMigrationStrategy.EfMigrations;

    public bool RequiresHumanProdDeploy { get; set; }

    /// <summary>JSON: per-environment appsettings merges and env vars.</summary>
    public string? ConfigTransformsJson { get; set; }

    /// <summary>JSON: logical connection name → secret ref per environment type.</summary>
    public string? ConnectionMappingsJson { get; set; }

    public Application Application { get; set; } = null!;
    public Repository? PrimaryRepository { get; set; }
}
