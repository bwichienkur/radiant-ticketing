namespace EnhancementHub.Application.Abstractions;

public interface IRequestCollaborationNotifier
{
    Task NotifyCommentAddedAsync(
        Guid enhancementRequestId,
        CollaborationCommentPayload comment,
        CancellationToken cancellationToken = default);

    Task NotifyAnalysisUpdatedAsync(
        Guid enhancementRequestId,
        int analysisVersion,
        CancellationToken cancellationToken = default);

    Task NotifyPresenceAsync(
        Guid enhancementRequestId,
        CollaborationPresencePayload presence,
        CancellationToken cancellationToken = default);
}

public sealed record CollaborationCommentPayload(
    Guid Id,
    string Content,
    string UserDisplayName,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record CollaborationPresencePayload(
    string ConnectionId,
    string UserDisplayName,
    bool IsActive);
