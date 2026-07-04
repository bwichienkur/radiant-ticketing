using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Commands;

public sealed record CancelEnhancementRequestCommand(Guid Id) : IRequest<Unit>;

public sealed class CancelEnhancementRequestCommandHandler
    : IRequestHandler<CancelEnhancementRequestCommand, Unit>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public CancelEnhancementRequestCommandHandler(
        IEnhancementHubDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<Unit> Handle(CancelEnhancementRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.EnhancementRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.Id);

        entity.Status = EnhancementRequestStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "Cancelled",
            nameof(EnhancementRequest),
            entity.Id,
            entity.Title,
            cancellationToken);

        return Unit.Value;
    }
}
