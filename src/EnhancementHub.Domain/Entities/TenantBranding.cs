using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class TenantBranding : BaseEntity
{
    public Guid TenantId { get; set; }
    public string? LogoUrl { get; set; }
    public string AccentColor { get; set; } = "#2563eb";
    public string? ProductName { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
