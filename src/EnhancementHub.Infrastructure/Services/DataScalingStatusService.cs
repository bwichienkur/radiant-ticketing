using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class DataScalingStatusService : IDataScalingStatusService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly DatabaseScalingOptions _scalingOptions;
    private readonly RetentionOptions _retentionOptions;

    public DataScalingStatusService(
        IEnhancementHubDbContext dbContext,
        IConfiguration configuration,
        IOptions<DatabaseScalingOptions> scalingOptions,
        IOptions<RetentionOptions> retentionOptions)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _scalingOptions = scalingOptions.Value;
        _retentionOptions = retentionOptions.Value;
    }

    public async Task<DataScalingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var vectorSearch = BuildVectorSearchStatus();
        var database = BuildDatabaseStatus();
        var archival = await BuildArchivalStatusAsync(cancellationToken);

        return new DataScalingStatusDto(
            DateTime.UtcNow,
            vectorSearch,
            database,
            archival);
    }

    private VectorSearchConfigurationStatusDto BuildVectorSearchStatus()
    {
        var provider = _configuration["VectorSearch:Provider"] ?? "InMemory";
        var dimensions = _configuration.GetValue("VectorSearch:Dimensions", 64);
        var issues = new List<ConfigurationValidationIssueDto>();
        string? recommended = null;

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            recommended = "Qdrant";
            issues.Add(new ConfigurationValidationIssueDto(
                "Warning",
                "InMemory vector search does not scale beyond a single process. Use Qdrant or AzureSearch for large deployments."));
        }
        else if (string.Equals(provider, "Qdrant", StringComparison.OrdinalIgnoreCase))
        {
            ValidateRequired("VectorSearch:Qdrant:Url", issues);
        }
        else if (string.Equals(provider, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        {
            ValidateRequired("VectorSearch:AzureSearch:Endpoint", issues);
            ValidateRequired("VectorSearch:AzureSearch:ApiKey", issues);
        }
        else if (string.Equals(provider, "PgVector", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsPostgreSql())
            {
                issues.Add(new ConfigurationValidationIssueDto(
                    "Error",
                    "PgVector requires Database:Provider=PostgreSQL."));
            }

            recommended = "Qdrant";
            issues.Add(new ConfigurationValidationIssueDto(
                "Info",
                "PgVector works for moderate scale. Consider Qdrant when indexing 100+ repositories."));
        }

        var isProductionReady = !string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase)
            && issues.All(i => !i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));

        return new VectorSearchConfigurationStatusDto(
            provider,
            isProductionReady,
            recommended,
            dimensions,
            issues);
    }

    private DatabaseConnectionScalingStatusDto BuildDatabaseStatus()
    {
        var primary = _configuration.GetConnectionString("Default") ?? string.Empty;
        var reporting = _configuration.GetConnectionString("Reporting") ?? primary;
        var readReplicaConfigured = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Reporting"))
            && !string.Equals(primary, reporting, StringComparison.Ordinal);

        return new DatabaseConnectionScalingStatusDto(
            readReplicaConfigured,
            "Default",
            readReplicaConfigured ? "Reporting" : "Default",
            _scalingOptions.MaxPoolSize,
            _scalingOptions.SchemaScanMaxConcurrency,
            _configuration["Database:Provider"] ?? "Sqlite");
    }

    private async Task<DataArchivalStatusDto> BuildArchivalStatusAsync(CancellationToken cancellationToken)
    {
        var auditLogCount = await _dbContext.AuditLogs.LongCountAsync(cancellationToken);
        var aiPromptRunCount = await _dbContext.AiPromptRuns.LongCountAsync(cancellationToken);

        var cutoff = _retentionOptions.AiPromptRunsDays > 0
            ? DateTime.UtcNow.AddDays(-_retentionOptions.AiPromptRunsDays)
            : (DateTime?)null;

        var eligibleArchive = cutoff.HasValue
            ? await _dbContext.AiPromptRuns.LongCountAsync(r => r.CreatedAt < cutoff.Value, cancellationToken)
            : 0;

        var recommendations = new List<string>();
        if (auditLogCount > 100_000)
        {
            recommendations.Add("Audit log volume is high — enable CSV/JSON export schedules and consider read replica for reporting.");
        }

        if (eligibleArchive > 0 && !_retentionOptions.ArchiveAiPromptRunsBeforeDelete)
        {
            recommendations.Add("Enable Retention:ArchiveAiPromptRunsBeforeDelete to export AI prompt runs before purge.");
        }

        if (!string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Reporting")))
        {
            recommendations.Add("Reporting connection configured — dashboard and AI usage queries use the read replica.");
        }

        return new DataArchivalStatusDto(
            auditLogCount,
            aiPromptRunCount,
            eligibleArchive,
            _retentionOptions.ArchiveAiPromptRunsBeforeDelete,
            _retentionOptions.ArchivePath,
            _retentionOptions.AiPromptRunsDays,
            recommendations);
    }

    private void ValidateRequired(string key, List<ConfigurationValidationIssueDto> issues)
    {
        if (string.IsNullOrWhiteSpace(_configuration[key]))
        {
            issues.Add(new ConfigurationValidationIssueDto("Error", $"{key} is required."));
        }
    }

    private bool IsPostgreSql()
    {
        var provider = _configuration["Database:Provider"];
        return string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase);
    }
}
