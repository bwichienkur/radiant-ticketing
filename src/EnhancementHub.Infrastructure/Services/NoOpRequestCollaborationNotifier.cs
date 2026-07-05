using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Services;

public sealed class NoOpRequestCollaborationNotifier : IRequestCollaborationNotifier
{
    public Task NotifyCommentAddedAsync(
        Guid enhancementRequestId,
        CollaborationCommentPayload comment,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task NotifyAnalysisUpdatedAsync(
        Guid enhancementRequestId,
        int analysisVersion,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task NotifyPresenceAsync(
        Guid enhancementRequestId,
        CollaborationPresencePayload presence,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
