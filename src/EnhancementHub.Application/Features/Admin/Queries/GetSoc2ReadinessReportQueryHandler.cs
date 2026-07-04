using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetSoc2ReadinessReportQueryHandler
    : IRequestHandler<GetSoc2ReadinessReportQuery, Soc2ReadinessReportDto>
{
    private readonly ISoc2ReadinessService _readinessService;

    public GetSoc2ReadinessReportQueryHandler(ISoc2ReadinessService readinessService) =>
        _readinessService = readinessService;

    public Task<Soc2ReadinessReportDto> Handle(
        GetSoc2ReadinessReportQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_readinessService.GetReport());
}
