using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ObservabilityStatusService : IObservabilityStatusService
{
    private readonly IConfiguration _configuration;
    private readonly ObservabilityOptions _observabilityOptions;
    private readonly DataProtectionStorageOptions _dataProtectionOptions;

    public ObservabilityStatusService(
        IConfiguration configuration,
        IOptions<ObservabilityOptions> observabilityOptions,
        IOptions<DataProtectionStorageOptions> dataProtectionOptions)
    {
        _configuration = configuration;
        _observabilityOptions = observabilityOptions.Value;
        _dataProtectionOptions = dataProtectionOptions.Value;
    }

    public Task<ObservabilityStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var openTelemetry = BuildOpenTelemetryStatus();
        var dataProtection = BuildDataProtectionStatus();
        var ha = BuildHighAvailabilityStatus(openTelemetry, dataProtection);

        return Task.FromResult(new ObservabilityStatusDto(
            DateTime.UtcNow,
            openTelemetry,
            dataProtection,
            ha));
    }

    private OpenTelemetryStatusDto BuildOpenTelemetryStatus()
    {
        var instrumentations = new List<string>();
        if (_observabilityOptions.InstrumentAspNetCore)
        {
            instrumentations.Add("AspNetCore");
        }

        if (_observabilityOptions.InstrumentHttpClient)
        {
            instrumentations.Add("HttpClient");
        }

        if (_observabilityOptions.InstrumentEntityFramework)
        {
            instrumentations.Add("EntityFrameworkCore");
        }

        if (_observabilityOptions.InstrumentBackgroundJobs)
        {
            instrumentations.Add("BackgroundJobs");
        }

        return new OpenTelemetryStatusDto(
            _observabilityOptions.Enabled,
            _observabilityOptions.ServiceName,
            _observabilityOptions.OtlpEndpoint,
            _observabilityOptions.EnablePrometheusMetrics,
            _observabilityOptions.InstrumentBackgroundJobs,
            instrumentations);
    }

    private DataProtectionStatusDto BuildDataProtectionStatus()
    {
        var provider = DataProtectionServiceExtensions
            .ResolveStorageProvider(_dataProtectionOptions)
            .ToString();

        var issues = new List<ConfigurationValidationIssueDto>();
        var sharedConfigured = false;

        if (string.Equals(provider, nameof(DataProtectionStorageProvider.AzureBlob), StringComparison.OrdinalIgnoreCase))
        {
            sharedConfigured = !string.IsNullOrWhiteSpace(_dataProtectionOptions.AzureBlob.ConnectionString);
            if (!sharedConfigured)
            {
                issues.Add(new ConfigurationValidationIssueDto(
                    "Error",
                    "DataProtection:AzureBlob:ConnectionString is required for AzureBlob storage."));
            }
        }
        else
        {
            sharedConfigured = !string.IsNullOrWhiteSpace(_dataProtectionOptions.KeysPath);
            if (!sharedConfigured)
            {
                issues.Add(new ConfigurationValidationIssueDto(
                    "Warning",
                    "Configure DataProtection:KeysPath on NFS/shared storage for multi-instance deployments."));
            }
            else
            {
                issues.Add(new ConfigurationValidationIssueDto(
                    "Info",
                    "Mount KeysPath on shared NFS/Azure Files so all API and Web instances share the key ring."));
            }
        }

        return new DataProtectionStatusDto(
            provider,
            sharedConfigured,
            _dataProtectionOptions.KeysPath,
            _dataProtectionOptions.AzureBlob.ContainerName,
            issues);
    }

    private HighAvailabilityReadinessDto BuildHighAvailabilityStatus(
        OpenTelemetryStatusDto openTelemetry,
        DataProtectionStatusDto dataProtection)
    {
        var postgres = IsPostgreSql();
        var hangfire = postgres
            && string.Equals(
                _configuration["BackgroundJobs:Provider"],
                "Hangfire",
                StringComparison.OrdinalIgnoreCase);

        var readReplica = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Reporting"));
        var vectorProvider = _configuration["VectorSearch:Provider"] ?? "InMemory";
        var vectorOffload = !string.Equals(vectorProvider, "InMemory", StringComparison.OrdinalIgnoreCase);

        var recommendations = new List<string>();
        if (!postgres)
        {
            recommendations.Add("Use PostgreSQL for production HA (Database:Provider=PostgreSQL).");
        }

        if (!hangfire)
        {
            recommendations.Add("Enable Hangfire (BackgroundJobs:Provider=Hangfire) for durable job orchestration.");
        }

        if (!readReplica)
        {
            recommendations.Add("Configure ConnectionStrings:Reporting for read-replica reporting offload.");
        }

        if (!vectorOffload)
        {
            recommendations.Add("Offload vector search to Qdrant or Azure Search at scale.");
        }

        if (!dataProtection.SharedKeyRingConfigured)
        {
            recommendations.Add("Configure shared Data Protection (Azure Blob or NFS KeysPath).");
        }

        if (!openTelemetry.Enabled)
        {
            recommendations.Add("Enable Observability:Enabled with OTLP endpoint for traces and metrics.");
        }

        return new HighAvailabilityReadinessDto(
            postgres,
            hangfire,
            readReplica,
            vectorOffload,
            dataProtection.SharedKeyRingConfigured,
            openTelemetry.Enabled,
            recommendations);
    }

    private bool IsPostgreSql()
    {
        var provider = _configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(provider))
        {
            return provider.Contains("postgres", StringComparison.OrdinalIgnoreCase);
        }

        var connection = _configuration.GetConnectionString("Default") ?? string.Empty;
        return connection.Contains("Host=", StringComparison.OrdinalIgnoreCase);
    }
}
