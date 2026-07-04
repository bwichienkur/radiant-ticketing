using EnhancementHub.Application.Abstractions;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed class RetryBackgroundJobCommandHandler
    : IRequestHandler<RetryBackgroundJobCommand, bool>
{
    private readonly IBackgroundJobStatusService _jobStatusService;

    public RetryBackgroundJobCommandHandler(IBackgroundJobStatusService jobStatusService) =>
        _jobStatusService = jobStatusService;

    public Task<bool> Handle(RetryBackgroundJobCommand request, CancellationToken cancellationToken) =>
        _jobStatusService.RetryFailedJobAsync(request.JobId, cancellationToken);
}
