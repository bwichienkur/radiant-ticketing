using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetBackgroundJobsStatusQueryHandler
    : IRequestHandler<GetBackgroundJobsStatusQuery, BackgroundJobsStatusDto>
{
    private readonly IBackgroundJobStatusService _jobStatusService;

    public GetBackgroundJobsStatusQueryHandler(IBackgroundJobStatusService jobStatusService) =>
        _jobStatusService = jobStatusService;

    public Task<BackgroundJobsStatusDto> Handle(
        GetBackgroundJobsStatusQuery request,
        CancellationToken cancellationToken) =>
        _jobStatusService.GetStatusAsync(cancellationToken);
}
