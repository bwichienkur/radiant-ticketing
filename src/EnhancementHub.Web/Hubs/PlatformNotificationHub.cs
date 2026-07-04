using Microsoft.AspNetCore.SignalR;

namespace EnhancementHub.Web.Hubs;

public sealed class PlatformNotificationHub : Hub
{
    public async Task JoinApplicationGroup(string applicationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"app:{applicationId}");
}
