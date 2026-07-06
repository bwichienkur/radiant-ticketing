using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class TenantDeploymentEnvironment : BaseEntity
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DeploymentEnvironmentType EnvironmentType { get; set; } = DeploymentEnvironmentType.Test;

    /// <summary>URL pattern, e.g. https://{appSlug}-test.contoso.com</summary>
    public string? BaseUrlTemplate { get; set; }

    /// <summary>Optional vault prefix override for this environment.</summary>
    public string? SecretReferencePrefix { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public bool RequiresApprovalForDeploy { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
