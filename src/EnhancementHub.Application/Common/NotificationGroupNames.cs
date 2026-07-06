namespace EnhancementHub.Application.Common;

public static class NotificationGroupNames
{
    public static string ForUser(Guid userId) => $"user:{userId}";
}
