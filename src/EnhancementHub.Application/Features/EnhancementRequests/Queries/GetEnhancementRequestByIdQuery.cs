using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;

namespace EnhancementHub.Application.Features.EnhancementRequests.Queries;

public sealed record GetEnhancementRequestByIdQuery(Guid Id) : IRequest<EnhancementRequestDetailDto>;

public sealed class GetEnhancementRequestByIdQueryHandler
    : IRequestHandler<GetEnhancementRequestByIdQuery, EnhancementRequestDetailDto>
{
    private readonly IEnhancementRequestRepository _requests;
    private readonly IEnhancementRequestAccessService _accessService;

    public GetEnhancementRequestByIdQueryHandler(
        IEnhancementRequestRepository requests,
        IEnhancementRequestAccessService accessService)
    {
        _requests = requests;
        _accessService = accessService;
    }

    public async Task<EnhancementRequestDetailDto> Handle(
        GetEnhancementRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.GetAccessibleRequestAsync(request.Id, cancellationToken);

        var entity = await _requests.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.Id);

        return entity.ToDetailDto();
    }
}
