namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record ObservabilityStatusDto(
    DateTime GeneratedAt,
    OpenTelemetryStatusDto OpenTelemetry,
    DataProtectionStatusDto DataProtection,
    HighAvailabilityReadinessDto HighAvailability);

public sealed record OpenTelemetryStatusDto(
    bool Enabled,
    string ServiceName,
    string? OtlpEndpoint,
    bool PrometheusMetricsEnabled,
    bool BackgroundJobInstrumentationEnabled,
    IReadOnlyList<string> ActiveInstrumentations);

public sealed record DataProtectionStatusDto(
    string StorageProvider,
    bool SharedKeyRingConfigured,
    string? KeysPath,
    string? AzureBlobContainer,
    IReadOnlyList<ConfigurationValidationIssueDto> Issues);

public sealed record HighAvailabilityReadinessDto(
    bool PostgresConfigured,
    bool HangfireConfigured,
    bool ReadReplicaConfigured,
    bool VectorOffloadConfigured,
    bool SharedDataProtectionConfigured,
    bool ObservabilityEnabled,
    IReadOnlyList<string> Recommendations);
