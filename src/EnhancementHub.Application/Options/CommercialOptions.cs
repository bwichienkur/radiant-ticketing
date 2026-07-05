namespace EnhancementHub.Application.Options;

public sealed class CommercialOptions
{
    public const string SectionName = "Commercial";

    public bool Enabled { get; set; } = true;
    public bool SelfServiceSignupEnabled { get; set; } = true;
    public int TrialDays { get; set; } = 14;
    public string DefaultRegion { get; set; } = "US";
    public TenantPlanLimits TrialLimits { get; set; } = new() { MaxApplications = 3, MaxAnalysesPerMonth = 50, MaxStorageMegabytes = 500 };
    public TenantPlanLimits TeamLimits { get; set; } = new() { MaxApplications = 25, MaxAnalysesPerMonth = 500, MaxStorageMegabytes = 5000 };
    public TenantPlanLimits EnterpriseLimits { get; set; } = new() { MaxApplications = 500, MaxAnalysesPerMonth = 10000, MaxStorageMegabytes = 100000 };
}

public sealed class TenantPlanLimits
{
    public int MaxApplications { get; set; }
    public int MaxAnalysesPerMonth { get; set; }
    public long MaxStorageMegabytes { get; set; }
}
