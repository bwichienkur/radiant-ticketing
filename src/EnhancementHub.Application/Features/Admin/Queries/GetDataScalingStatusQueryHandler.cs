using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetDataScalingStatusQueryHandler
    : IRequestHandler<GetDataScalingStatusQuery, DataScalingStatusDto>
{
    private readonly IDataScalingStatusService _statusService;

    public GetDataScalingStatusQueryHandler(IDataScalingStatusService statusService) =>
        _statusService = statusService;

    public Task<DataScalingStatusDto> Handle(
        GetDataScalingStatusQuery request,
        CancellationToken cancellationToken) =>
        _statusService.GetStatusAsync(cancellationToken);
}
