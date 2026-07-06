using System.Text.Json;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.Delivery;

internal static class DeliveryTimeline
{
    public static string AppendEvent(string? timelineJson, string message)
    {
        var events = Deserialize(timelineJson).ToList();
        events.Add(new DeliveryTimelineEvent(DateTime.UtcNow, message));
        return JsonSerializer.Serialize(events);
    }

    public static IReadOnlyList<DeliveryTimelineEvent> Deserialize(string? timelineJson)
    {
        if (string.IsNullOrWhiteSpace(timelineJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<DeliveryTimelineEvent>>(timelineJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal sealed record DeliveryTimelineEvent(DateTime OccurredAt, string Message);
}

internal static class DeliveryStatusMapper
{
    public static EnhancementRequestStatus MapPhaseToRequestStatus(DeliveryRunPhase phase) =>
        phase switch
        {
            DeliveryRunPhase.Pending or DeliveryRunPhase.Implementing or DeliveryRunPhase.AwaitingPullRequestReview
                => EnhancementRequestStatus.Implementing,
            DeliveryRunPhase.DeployingToTest => EnhancementRequestStatus.InTest,
            DeliveryRunPhase.RunningQa => EnhancementRequestStatus.QaInProgress,
            DeliveryRunPhase.AwaitingUat => EnhancementRequestStatus.AwaitingUat,
            DeliveryRunPhase.UatApproved => EnhancementRequestStatus.UatApproved,
            DeliveryRunPhase.ProdScheduled => EnhancementRequestStatus.ProdScheduled,
            DeliveryRunPhase.DeployingToProduction => EnhancementRequestStatus.DeployingToProduction,
            DeliveryRunPhase.Completed => EnhancementRequestStatus.Completed,
            DeliveryRunPhase.RolledBack => EnhancementRequestStatus.InProgress,
            DeliveryRunPhase.Failed => EnhancementRequestStatus.InProgress,
            _ => EnhancementRequestStatus.InProgress
        };
}

internal static class RepositoryCoordinates
{
    public static (string Owner, string Repo) Resolve(string? repositoryUrl, string repositoryName, string? demoOwner)
    {
        if (!string.IsNullOrWhiteSpace(repositoryUrl)
            && Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri)
            && uri.Host.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return (parts[0], parts[1].Replace(".git", "", StringComparison.OrdinalIgnoreCase));
            }
        }

        var owner = string.IsNullOrWhiteSpace(demoOwner) ? "enhancementhub-demo" : demoOwner;
        var repo = repositoryName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
        return (owner, repo);
    }
}
