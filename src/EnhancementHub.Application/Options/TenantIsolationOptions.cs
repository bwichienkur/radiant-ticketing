namespace EnhancementHub.Application.Options;

public sealed class TenantIsolationOptions
{
    public const string SectionName = "TenantIsolation";

    public bool Enabled { get; set; }
    public string SchemaPrefix { get; set; } = "tenant_";
    public bool AutoProvisionEnterprise { get; set; } = true;
    public bool AutoProvisionEuRegion { get; set; }
    public string[] ControlPlaneTables { get; set; } =
    [
        "Tenants",
        "TenantUsageSnapshots",
        "__EFMigrationsHistory"
    ];
}
