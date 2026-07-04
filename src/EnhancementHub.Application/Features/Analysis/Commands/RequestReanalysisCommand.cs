using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Analysis.Commands;

public sealed record RequestReanalysisCommand(Guid EnhancementRequestId) : IRequest<EnhancementAnalysisDto>;

public sealed class RequestReanalysisCommandHandler
    : IRequestHandler<RequestReanalysisCommand, EnhancementAnalysisDto>
{
    private readonly IMediator _mediator;
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public RequestReanalysisCommandHandler(
        IMediator mediator,
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<EnhancementAnalysisDto> Handle(
        RequestReanalysisCommand request,
        CancellationToken cancellationToken)
    {
        var enhancementRequest = await _dbContext.EnhancementRequests
            .FirstOrDefaultAsync(r => r.Id == request.EnhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.EnhancementRequestId);

        var previousStatus = enhancementRequest.Status.ToString();
        enhancementRequest.Status = EnhancementRequestStatus.Submitted;
        enhancementRequest.UpdatedAt = DateTime.UtcNow;

        if (_currentUser.UserId.HasValue)
        {
            _dbContext.ApprovalActions.Add(new ApprovalAction
            {
                Id = Guid.NewGuid(),
                EnhancementRequestId = enhancementRequest.Id,
                UserId = _currentUser.UserId.Value,
                ActionType = ApprovalActionType.SendForReanalysis,
                PreviousValue = previousStatus,
                NewValue = EnhancementRequestStatus.Submitted.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ReanalysisRequested",
            nameof(EnhancementRequest),
            enhancementRequest.Id,
            "Enhancement request sent for re-analysis",
            cancellationToken);

        return await _mediator.Send(new TriggerAiAnalysisCommand(request.EnhancementRequestId), cancellationToken);
    }
}
