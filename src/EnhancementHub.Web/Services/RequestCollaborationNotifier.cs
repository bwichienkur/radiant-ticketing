using EnhancementHub.Application.Common;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnhancementHub.Web.Services;

public sealed class RequestCollaborationNotifier : IRequestCollaborationNotifier
{
    private readonly IHubContext<EnhancementRequestCollaborationHub> _hubContext;

    public RequestCollaborationNotifier(IHubContext<EnhancementRequestCollaborationHub> hubContext) =>
        _hubContext = hubContext;

    public Task NotifyCommentAddedAsync(
        Guid enhancementRequestId,
        CollaborationCommentPayload comment,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(CollaborationGroupNames.ForRequest(enhancementRequestId))
            .SendAsync("CommentAdded", comment, cancellationToken);

    public Task NotifyAnalysisUpdatedAsync(
        Guid enhancementRequestId,
        int analysisVersion,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(CollaborationGroupNames.ForRequest(enhancementRequestId))
            .SendAsync("AnalysisUpdated", new { version = analysisVersion }, cancellationToken);

    public Task NotifyPresenceAsync(
        Guid enhancementRequestId,
        CollaborationPresencePayload presence,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(CollaborationGroupNames.ForRequest(enhancementRequestId))
            .SendAsync("PresenceUpdated", presence, cancellationToken);
}
