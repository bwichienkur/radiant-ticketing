using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

/// <summary>
/// Tenant-wide defaults for automated delivery orchestration (Phase A — config only).
/// </summary>
public class TenantDeliveryProfile : BaseEntity
{
    public Guid TenantId { get; set; }

    public CicdProvider DefaultCicdProvider { get; set; } = CicdProvider.GitHubActions;

    /// <summary>Prefix for vault secret references, e.g. kv://contoso-delivery/</summary>
    public string? VaultSecretPrefix { get; set; }

    public bool AutoImplementOnApprove { get; set; }

    public bool AutoDeployToTest { get; set; }

    public bool RequirePullRequestReview { get; set; } = true;

    public bool RequireUatSignoff { get; set; } = true;

    public bool RequireProdChangeWindow { get; set; } = true;

    public string? ChangeWindowNotes { get; set; }

    public int QaVideoRetentionDays { get; set; } = 90;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<TenantDeploymentEnvironment> Environments { get; set; } = new List<TenantDeploymentEnvironment>();
}
