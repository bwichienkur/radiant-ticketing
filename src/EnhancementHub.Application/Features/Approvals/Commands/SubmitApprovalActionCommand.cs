using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Approvals.Commands;

public sealed record SubmitApprovalActionCommand(
    Guid EnhancementRequestId,
    ApprovalActionType ActionType,
    string? Comments,
    Guid? EnhancementAnalysisId = null) : IRequest<ApprovalActionDto>;

public sealed class SubmitApprovalActionCommandHandler
    : IRequestHandler<SubmitApprovalActionCommand, ApprovalActionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public SubmitApprovalActionCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<ApprovalActionDto> Handle(
        SubmitApprovalActionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User must be authenticated to submit approval actions.");
        }

        if (request.ActionType is ApprovalActionType.Approve or ApprovalActionType.Reject)
        {
            if (_currentUser.Role is not (UserRole.Approver or UserRole.Admin))
            {
                throw new ForbiddenException(
                    "Only users with Approver or Admin roles can approve or reject enhancement requests.");
            }
        }

        var enhancementRequest = await _dbContext.EnhancementRequests
            .FirstOrDefaultAsync(r => r.Id == request.EnhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.EnhancementRequestId);

        var previousStatus = enhancementRequest.Status.ToString();
        var newStatus = MapActionToStatus(request.ActionType, enhancementRequest.Status);

        if (newStatus.HasValue)
        {
            enhancementRequest.Status = newStatus.Value;
            enhancementRequest.UpdatedAt = DateTime.UtcNow;
        }

        var action = new ApprovalAction
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.EnhancementRequestId,
            EnhancementAnalysisId = request.EnhancementAnalysisId,
            UserId = _currentUser.UserId.Value,
            ActionType = request.ActionType,
            Comments = request.Comments,
            PreviousValue = previousStatus,
            NewValue = newStatus?.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ApprovalActions.Add(action);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            request.ActionType.ToString(),
            nameof(EnhancementRequest),
            enhancementRequest.Id,
            request.Comments ?? $"Status changed from {previousStatus} to {newStatus?.ToString() ?? previousStatus}",
            cancellationToken);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        action.User = user;
        return action.ToDto();
    }

    private static EnhancementRequestStatus? MapActionToStatus(
        ApprovalActionType actionType,
        EnhancementRequestStatus currentStatus)
    {
        return actionType switch
        {
            ApprovalActionType.Approve => EnhancementRequestStatus.Approved,
            ApprovalActionType.Reject => EnhancementRequestStatus.Rejected,
            ApprovalActionType.RequestClarification => EnhancementRequestStatus.NeedsClarification,
            ApprovalActionType.MarkReadyForDevelopment => EnhancementRequestStatus.ReadyForDevelopment,
            ApprovalActionType.SendForReanalysis => EnhancementRequestStatus.Submitted,
            _ => null
        };
    }
}
