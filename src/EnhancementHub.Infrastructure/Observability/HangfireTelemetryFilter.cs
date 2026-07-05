using System.Diagnostics;
using EnhancementHub.Infrastructure.Observability;
using Hangfire.Common;
using Hangfire.Server;

namespace EnhancementHub.Infrastructure.Observability;

public sealed class HangfireTelemetryFilter : JobFilterAttribute, IServerFilter
{
    private const string ActivityKey = "EnhancementHub.Activity";
    private const string StartedKey = "EnhancementHub.StartedUtc";

    public void OnPerforming(PerformingContext filterContext)
    {
        var jobName = filterContext.BackgroundJob?.Job?.Type.Name ?? "unknown";
        var activity = EnhancementHubTelemetry.ActivitySource.StartActivity(
            $"job {jobName}",
            ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("hangfire.job.id", filterContext.BackgroundJob?.Id);
            activity.SetTag("hangfire.job.type", jobName);
        }

        filterContext.Items[ActivityKey] = activity;
        filterContext.Items[StartedKey] = DateTime.UtcNow;
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var started = filterContext.Items.TryGetValue(StartedKey, out var startedValue)
            && startedValue is DateTime startedUtc
            ? startedUtc
            : DateTime.UtcNow;

        var duration = (DateTime.UtcNow - started).TotalSeconds;
        var jobName = filterContext.BackgroundJob?.Job?.Type.Name ?? "unknown";
        var tags = new KeyValuePair<string, object?>("job", jobName);

        EnhancementHubTelemetry.JobDurationSeconds.Record(duration, tags);

        if (filterContext.Exception is null)
        {
            EnhancementHubTelemetry.JobCompletedTotal.Add(1, tags);
        }
        else
        {
            EnhancementHubTelemetry.JobFailedTotal.Add(1, tags);
        }

        if (filterContext.Items.TryGetValue(ActivityKey, out var activityObj)
            && activityObj is Activity activity)
        {
            if (filterContext.Exception is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, filterContext.Exception.Message);
            }

            activity.Dispose();
        }
    }
}
