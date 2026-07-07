using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;

namespace EnhancementHub.Application.Features.EnhancementRequests.Commands;

public sealed record CancelEnhancementRequestCommand(Guid Id) : IRequest<Unit>;

public sealed class CancelEnhancementRequestCommandHandler
    : IRequestHandler<CancelEnhancementRequestCommand, Unit>
{
    private readonly IEnhancementRequestRepository _requests;
    private readonly IAuditService _auditService;
    private readonly IEnhancementRequestAccessService _accessService;

    public CancelEnhancementRequestCommandHandler(
        IEnhancementRequestRepository requests,
        IAuditService auditService,
        IEnhancementRequestAccessService accessService)
    {
        _requests = requests;
        _auditService = auditService;
        _accessService = accessService;
    }

    public async Task<Unit> Handle(CancelEnhancementRequestCommand request, CancellationToken cancellationToken)
    {
        await _accessService.EnsureCanModifyAsync(request.Id, cancellationToken);

        var entity = await _requests.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.Id);

        entity.Status = EnhancementRequestStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "Cancelled",
            nameof(EnhancementRequest),
            entity.Id,
            entity.Title,
            cancellationToken);

        return Unit.Value;
    }
}
