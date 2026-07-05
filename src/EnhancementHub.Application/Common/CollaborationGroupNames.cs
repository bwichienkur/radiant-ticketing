namespace EnhancementHub.Application.Common;

public static class CollaborationGroupNames
{
    public static string ForRequest(Guid requestId) => $"request:{requestId}";
}
