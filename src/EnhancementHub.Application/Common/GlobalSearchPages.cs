namespace EnhancementHub.Application.Common;

public static class GlobalSearchPages
{
    public static IReadOnlyList<GlobalSearchPageDefinition> All { get; } =
    [
        new("Dashboard", "/Index", ["home", "overview", "metrics"]),
        new("Enhancement Requests", "/Spa/RequestList", ["requests", "intake"]),
        new("Approval Queue", "/Spa/ApprovalQueue", ["approve", "pending", "approval"]),
        new("New Request", "/Spa/CreateRequest", ["create", "submit", "new"]),
        new("System Map", "/Spa/SystemMap", ["graph", "architecture", "map"]),
        new("Onboarding Wizard", "/Spa/OnboardingWizard", ["setup", "onboard", "wizard"]),
        new("Applications", "/Spa/Applications", ["applications", "systems", "apps"]),
        new("Repositories", "/Spa/Repositories", ["repos", "git", "index"]),
        new("Audit Log", "/Spa/Audit", ["audit", "compliance", "history"]),
        new("Schema Drift", "/Spa/SchemaDrift", ["drift", "schema"]),
        new("Global Search", "/Spa/Search", ["search", "find"]),
        new("ROI Dashboard", "/Admin/Roi", ["roi", "metrics", "admin"]),
        new("Tenancy & Billing", "/Admin/Tenancy", ["tenant", "billing", "commercial"]),
        new("Admin Settings", "/Admin/Settings", ["admin", "settings"]),
        new("Notification Preferences", "/Account/NotificationPreferences", ["notifications", "email", "alerts"])
    ];
}

public sealed record GlobalSearchPageDefinition(string Title, string Url, string[] Keywords);
