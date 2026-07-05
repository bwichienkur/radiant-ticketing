using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class TenantUsageSnapshot : BaseEntity
{
    public Guid TenantId { get; set; }
    public DateTime PeriodStart { get; set; }
    public int ApplicationCount { get; set; }
    public int AnalysisCount { get; set; }
    public long StorageBytes { get; set; }
    public DateTime CapturedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
