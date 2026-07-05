namespace EnhancementHub.Application.Features.Integrations.Dtos;

public sealed record OpenApiRegistrationDto(
    Guid Id,
    Guid ApplicationId,
    string Name,
    int EndpointCount,
    string? BaseUrl,
    DateTime? LastIngestedAt);

public sealed record OpenApiEndpointDto(
    Guid Id,
    string Path,
    string HttpMethod,
    string? OperationId,
    string? Summary,
    string? Tags);

public sealed record IntegrationStatusDto(
    bool GitHubWebhooksEnabled,
    bool SlackIntakeEnabled,
    bool TeamsIntakeEnabled,
    bool PolyglotIngestionEnabled,
    bool ServiceNowEnabled,
    IReadOnlyList<string> SupportedPolyglotLanguages);
