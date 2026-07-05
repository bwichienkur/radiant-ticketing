using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed record GetObservabilityStatusQuery : IRequest<ObservabilityStatusDto>;

public sealed class GetObservabilityStatusQueryHandler
    : IRequestHandler<GetObservabilityStatusQuery, ObservabilityStatusDto>
{
    private readonly IObservabilityStatusService _statusService;

    public GetObservabilityStatusQueryHandler(IObservabilityStatusService statusService) =>
        _statusService = statusService;

    public Task<ObservabilityStatusDto> Handle(
        GetObservabilityStatusQuery request,
        CancellationToken cancellationToken) =>
        _statusService.GetStatusAsync(cancellationToken);
}
