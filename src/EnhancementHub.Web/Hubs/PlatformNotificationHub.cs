using EnhancementHub.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EnhancementHub.Web.Hubs;

[Authorize]
public sealed class PlatformNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroupNames.ForUser(userId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinApplicationGroup(string applicationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"app:{applicationId}");
}
