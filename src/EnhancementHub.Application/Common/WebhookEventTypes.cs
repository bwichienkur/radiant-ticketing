namespace EnhancementHub.Application.Common;

public static class WebhookEventTypes
{
    public const string RequestApproved = "request.approved";
    public const string AnalysisCompleted = "analysis.completed";
    public const string DriftDetected = "drift.detected";

    public static IReadOnlyList<string> All { get; } =
    [
        RequestApproved,
        AnalysisCompleted,
        DriftDetected
    ];

    public static bool Matches(string subscriptionEventTypes, string eventType)
    {
        if (string.IsNullOrWhiteSpace(subscriptionEventTypes))
        {
            return false;
        }

        return subscriptionEventTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(type => string.Equals(type, eventType, StringComparison.OrdinalIgnoreCase));
    }
}
