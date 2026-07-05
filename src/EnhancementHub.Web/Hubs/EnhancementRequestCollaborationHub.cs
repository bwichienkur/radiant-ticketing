using EnhancementHub.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EnhancementHub.Web.Hubs;

[Authorize]
public sealed class EnhancementRequestCollaborationHub : Hub
{
    public async Task JoinRequest(string requestId)
    {
        if (!Guid.TryParse(requestId, out _))
        {
            throw new HubException("Invalid request id.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, CollaborationGroupNames.ForRequest(Guid.Parse(requestId)));
        await Clients.OthersInGroup(CollaborationGroupNames.ForRequest(Guid.Parse(requestId)))
            .SendAsync("UserJoined", new
            {
                connectionId = Context.ConnectionId,
                userName = Context.User?.Identity?.Name ?? "User"
            });
    }

    public async Task LeaveRequest(string requestId)
    {
        if (!Guid.TryParse(requestId, out var id))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, CollaborationGroupNames.ForRequest(id));
        await Clients.OthersInGroup(CollaborationGroupNames.ForRequest(id))
            .SendAsync("UserLeft", new
            {
                connectionId = Context.ConnectionId,
                userName = Context.User?.Identity?.Name ?? "User"
            });
    }

    public async Task UpdatePresence(string requestId, bool isActive)
    {
        if (!Guid.TryParse(requestId, out var id))
        {
            return;
        }

        await Clients.OthersInGroup(CollaborationGroupNames.ForRequest(id))
            .SendAsync("PresenceUpdated", new
            {
                connectionId = Context.ConnectionId,
                userName = Context.User?.Identity?.Name ?? "User",
                isActive
            });
    }
}
