using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetDataRetentionStatusQueryHandler
    : IRequestHandler<GetDataRetentionStatusQuery, DataRetentionStatusDto>
{
    private readonly IDataRetentionService _retentionService;

    public GetDataRetentionStatusQueryHandler(IDataRetentionService retentionService) =>
        _retentionService = retentionService;

    public Task<DataRetentionStatusDto> Handle(
        GetDataRetentionStatusQuery request,
        CancellationToken cancellationToken) =>
        _retentionService.GetStatusAsync(cancellationToken);
}
