using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IBackgroundJobStatusService
{
    Task<BackgroundJobsStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<bool> RetryFailedJobAsync(string jobId, CancellationToken cancellationToken = default);
}
