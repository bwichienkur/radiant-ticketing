using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EnhancementHub.Infrastructure.Services;

public sealed class Soc2ReadinessService : ISoc2ReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public Soc2ReadinessService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public Soc2ReadinessReportDto GetReport()
    {
        var controls = new List<Soc2ControlStatusDto>
        {
            Build(
                "CC6.1",
                "Security",
                "Logical access — authentication",
                "JWT bearer auth, cookie sessions, optional Entra ID OIDC",
                HasStrongJwtSecret() && (IsOidcConfigured() || _environment.IsDevelopment())
                    ? "Implemented"
                    : IsOidcEnabledButIncomplete() ? "Partial" : "Partial",
                "Set Jwt:Secret (≥32 chars) and enable OIDC for production SSO."),

            Build(
                "CC6.1",
                "Security",
                "Logical access — authorization",
                "Role-based access (Admin, Approver, Developer) + resource scoping",
                "Implemented",
                "Use Entra group → role mappings documented in docs/ENTRA_ID_SSO.md."),

            Build(
                "CC6.6",
                "Security",
                "Encryption at rest — secrets",
                "Data Protection key ring, encrypted DB connection strings, protected agent API keys",
                HasDataProtectionKeysPath() ? "Implemented" : "Partial",
                "Configure DataProtection:KeysPath on shared storage in Production."),

            Build(
                "CC6.6",
                "Security",
                "Encryption at rest — attachments",
                "Local disk or S3-compatible object storage abstraction",
                IsS3StorageConfigured() ? "Implemented" : "Partial",
                "Set Storage:Provider=S3 for multi-instance deployments."),

            Build(
                "CC6.7",
                "Security",
                "Transmission confidentiality",
                "HTTPS termination (reverse proxy / platform), TLS for external AI and DB connections",
                "Partial",
                "Terminate TLS at ingress; enforce HTTPS in Production deployments."),

            Build(
                "CC6.8",
                "Security",
                "Malware controls — attachments",
                "Extension whitelist, magic-byte validation, optional ClamAV scanning",
                IsClamAvEnabled() ? "Implemented" : "Partial",
                "Enable Attachments:Scanning:ClamAv for regulated environments."),

            Build(
                "CC7.2",
                "Security",
                "System monitoring",
                "Health checks (/health, /health/ready), structured logging, Hangfire job stats",
                "Implemented",
                "Integrate health endpoints with your observability platform."),

            Build(
                "CC7.2",
                "Security",
                "Audit logging",
                "Immutable AuditLog entity, export API (CSV/JSON), correlation IDs",
                "Implemented",
                "Export audit logs periodically via GET /api/auditlogs/export."),

            Build(
                "CC7.3",
                "Security",
                "Security event evaluation",
                "Admin authentication validation, failed job visibility, AI usage reporting",
                "Implemented",
                "Review /Admin/Authentication and /Admin/Jobs regularly."),

            Build(
                "CC6.5",
                "Security",
                "Data disposal",
                "Configurable retention for AiPromptRun and attachments",
                IsRetentionEnabled() ? "Implemented" : "Partial",
                "Set Retention:Enabled=true with AiPromptRunsDays and AttachmentsDays."),

            Build(
                "CC6.1",
                "Security",
                "AI data handling",
                "PII redaction before prompts, token/cost budgets, prompt run audit trail",
                IsAiBudgetConfigured() ? "Implemented" : "Partial",
                "Configure AI:Budget limits and review AiPromptRun retention."),

            Build(
                "CC6.1",
                "Security",
                "On-prem agent authentication",
                "Hashed agent API keys (X-Agent-Api-Key), registry in database",
                "Implemented",
                "Rotate agent keys via admin registration; store keys in a secrets manager."),

            Build(
                "CC6.1",
                "Security",
                "Rate limiting",
                "Login, attachment upload, and AI analysis trigger rate limits",
                "Implemented",
                null),

            Build(
                "CC8.1",
                "Security",
                "Change management",
                "EF migrations, CI tests, phased deployment docs",
                "Partial",
                "Pair EnhancementHub releases with your org change-control process."),

            Build(
                "A1.2",
                "Availability",
                "Recovery and job durability",
                "Hangfire + PostgreSQL job queue, manual retry, Worker-only job execution",
                IsHangfireConfigured() ? "Implemented" : "Partial",
                "Set BackgroundJobs:Provider=Hangfire with PostgreSQL in Production."),

            Build(
                "C1.2",
                "Confidentiality",
                "Data access scoping",
                "IEnhancementRequestAccessService, IApplicationAccessService team scoping",
                "Implemented",
                null),

            Build(
                "P1.1",
                "Privacy",
                "PII in AI workflows",
                "PiiRedactionService (email, phone, SSN, card patterns)",
                _configuration.GetValue("AI:PiiRedactionEnabled", true) ? "Implemented" : "Partial",
                "Keep AI:PiiRedactionEnabled=true unless explicitly approved otherwise.")
        };

        return new Soc2ReadinessReportDto(
            controls.Count(c => c.Status == "Implemented"),
            controls.Count(c => c.Status == "Partial"),
            controls.Count(c => c.Status == "Gap"),
            controls);
    }

    private bool HasStrongJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];
        return !string.IsNullOrWhiteSpace(secret)
            && secret != ProductionConfigurationValidator.DevJwtSecret
            && secret.Length >= 32;
    }

    private bool IsOidcEnabled() =>
        _configuration.GetValue<bool>("Authentication:OpenIdConnect:Enabled");

    private bool IsOidcConfigured() =>
        IsOidcEnabled()
        && !string.IsNullOrWhiteSpace(_configuration["Authentication:OpenIdConnect:Authority"])
        && !string.IsNullOrWhiteSpace(_configuration["Authentication:OpenIdConnect:ClientId"])
        && !string.IsNullOrWhiteSpace(_configuration["Authentication:OpenIdConnect:ClientSecret"]);

    private bool IsOidcEnabledButIncomplete() => IsOidcEnabled() && !IsOidcConfigured();

    private bool HasDataProtectionKeysPath() =>
        !string.IsNullOrWhiteSpace(_configuration["DataProtection:KeysPath"]);

    private bool IsS3StorageConfigured() =>
        string.Equals(_configuration["Storage:Provider"], "S3", StringComparison.OrdinalIgnoreCase);

    private bool IsClamAvEnabled() =>
        _configuration.GetValue("Attachments:Scanning:ClamAv:Enabled", false);

    private bool IsRetentionEnabled() =>
        _configuration.GetValue("Retention:Enabled", false);

    private bool IsAiBudgetConfigured() =>
        _configuration.GetValue("AI:Budget:Enabled", false)
        || _configuration.GetValue("AI:Budget:DailyTokenLimit", 0) > 0;

    private bool IsHangfireConfigured() =>
        string.Equals(_configuration["BackgroundJobs:Provider"], "Hangfire", StringComparison.OrdinalIgnoreCase)
        && string.Equals(_configuration["Database:Provider"], "PostgreSQL", StringComparison.OrdinalIgnoreCase);

    private static Soc2ControlStatusDto Build(
        string controlId,
        string category,
        string title,
        string feature,
        string status,
        string? hint) =>
        new(controlId, category, title, feature, status, hint);
}
