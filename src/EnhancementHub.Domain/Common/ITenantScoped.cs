namespace EnhancementHub.Domain.Common;

/// <summary>Entities filtered by the current tenant when tenant context is active.</summary>
public interface ITenantScoped
{
    Guid? TenantId { get; set; }
}
