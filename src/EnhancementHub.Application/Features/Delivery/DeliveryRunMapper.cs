using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Features.Delivery;

public static class DeliveryRunMapper
{
    public static async Task<EnhancementDeliveryRunDto> ToDtoAsync(
        EnhancementDeliveryRun run,
        IFileStorageService? fileStorage,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QaTestStepDto> qaSteps;
        if (string.IsNullOrWhiteSpace(run.QaStepsJson))
        {
            qaSteps = Array.Empty<QaTestStepDto>();
        }
        else
        {
            qaSteps = JsonSerializer.Deserialize<List<QaStepJson>>(run.QaStepsJson)?
                .Select(s => new QaTestStepDto(s.Step, s.Passed, s.Detail))
                .ToList()
                ?? new List<QaTestStepDto>();
        }

        var timeline = DeserializeTimeline(run.TimelineJson);
        var videoUrl = await PresignAsync(fileStorage, run.QaVideoStoragePath, cancellationToken);
        var reportUrl = await PresignAsync(fileStorage, run.QaReportStoragePath, cancellationToken);

        return new EnhancementDeliveryRunDto(
            run.Id,
            run.EnhancementRequestId,
            run.RunNumber,
            run.Phase,
            run.IsSimulation,
            run.BranchName,
            run.PullRequestUrl,
            run.PullRequestNumber,
            run.TestUrl,
            run.TestDeployReference,
            qaSteps,
            run.QaPassed,
            videoUrl,
            reportUrl,
            run.UatApproved,
            run.UatSignedOffAt,
            run.UatNotes,
            run.ProdScheduledAt,
            run.ProdDeployReference,
            run.ProdDeployedAt,
            timeline,
            run.LastError);
    }

    private static async Task<string?> PresignAsync(
        IFileStorageService? fileStorage,
        string? path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (fileStorage is not null)
        {
            var url = await fileStorage.GetPresignedDownloadUrlAsync(path, TimeSpan.FromHours(2), cancellationToken);
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return $"/web-api/spa/delivery/artifacts?path={Uri.EscapeDataString(path)}";
    }

    private static IReadOnlyList<DeliveryTimelineEventDto> DeserializeTimeline(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TimelineJson>>(json)?
                .Select(e => new DeliveryTimelineEventDto(e.OccurredAt, e.Message))
                .ToList()
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed class QaStepJson
    {
        public string Step { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    private sealed class TimelineJson
    {
        public DateTime OccurredAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
