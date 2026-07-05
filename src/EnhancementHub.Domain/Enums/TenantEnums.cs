namespace EnhancementHub.Domain.Enums;

public enum TenantPlan
{
    Trial = 0,
    Team = 1,
    Enterprise = 2
}

public enum TenantRegion
{
    US = 0,
    EU = 1,
    APAC = 2
}

public enum TenantSubscriptionStatus
{
    None = 0,
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Unpaid = 5
}

public enum TenantIsolationMode
{
    SharedRowLevel = 0,
    DedicatedSchema = 1
}
