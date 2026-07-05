namespace EnhancementHub.Application.Abstractions;

public interface IOpenApiIngestionService
{
    Task<OpenApiIngestionResult> IngestAsync(Guid registrationId, CancellationToken cancellationToken = default);

    Task<OpenApiIngestionResult> ParseAndValidateAsync(string specDocument, CancellationToken cancellationToken = default);
}

public sealed record OpenApiIngestionResult(
    bool Succeeded,
    int EndpointCount,
    string? ErrorMessage);

public interface IPolyglotSymbolIngestionService
{
    Task<PolyglotIngestionResult> IngestAsync(
        Guid repositoryId,
        IReadOnlyList<PolyglotSymbolInput> symbols,
        string language,
        CancellationToken cancellationToken = default);
}

public sealed record PolyglotSymbolInput(
    string FilePath,
    string SymbolName,
    string SymbolKind,
    int LineStart,
    int LineEnd,
    string? Summary);

public sealed record PolyglotIngestionResult(
    bool Succeeded,
    int SymbolsIngested,
    string? ErrorMessage);

public interface IChatIntakeService
{
    Task<ChatIntakeResult> SubmitFromSlackAsync(SlackIntakePayload payload, CancellationToken cancellationToken = default);

    Task<ChatIntakeResult> SubmitFromTeamsAsync(TeamsIntakePayload payload, CancellationToken cancellationToken = default);
}

public sealed record SlackIntakePayload(
    string Text,
    string? UserName,
    string? ChannelName,
    string? ResponseUrl);

public sealed record TeamsIntakePayload(
    string Text,
    string? UserName,
    Guid? TargetApplicationId,
    Guid? TeamId);

public sealed record ChatIntakeResult(
    bool Succeeded,
    Guid? EnhancementRequestId,
    string? Message);

public interface IGitHubWebhookService
{
    Task<GitHubWebhookResult> HandlePushAsync(
        string payload,
        string? signature256,
        CancellationToken cancellationToken = default);
}

public sealed record GitHubWebhookResult(
    bool Accepted,
    int RepositoriesQueued,
    string? Message);

public interface IServiceNowSyncService
{
    Task<ServiceNowSyncResult> ApplyInboundUpdateAsync(
        ServiceNowInboundUpdate update,
        CancellationToken cancellationToken = default);
}

public sealed record ServiceNowInboundUpdate(
    string ExternalId,
    string State,
    string? ShortDescription);

public sealed record ServiceNowSyncResult(
    bool Succeeded,
    Guid? EnhancementRequestId,
    string? Message);
