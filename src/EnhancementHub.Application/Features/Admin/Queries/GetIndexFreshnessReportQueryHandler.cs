using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetIndexFreshnessReportQueryHandler
    : IRequestHandler<GetIndexFreshnessReportQuery, IndexFreshnessReportDto>
{
    private readonly IIndexFreshnessService _freshnessService;

    public GetIndexFreshnessReportQueryHandler(IIndexFreshnessService freshnessService) =>
        _freshnessService = freshnessService;

    public Task<IndexFreshnessReportDto> Handle(
        GetIndexFreshnessReportQuery request,
        CancellationToken cancellationToken) =>
        _freshnessService.GetReportAsync(cancellationToken);
}
