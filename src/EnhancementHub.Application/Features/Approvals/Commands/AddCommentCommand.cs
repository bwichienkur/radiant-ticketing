using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Approvals.Commands;

public sealed record AddCommentCommand(
    Guid EnhancementRequestId,
    string Content,
    bool IsInternal = false) : IRequest<ApprovalActionDto>;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, ApprovalActionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public AddCommentCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<ApprovalActionDto> Handle(
        AddCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User must be authenticated to add comments.");
        }

        var exists = await _dbContext.EnhancementRequests
            .AnyAsync(r => r.Id == request.EnhancementRequestId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(nameof(EnhancementRequest), request.EnhancementRequestId);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.EnhancementRequestId,
            UserId = _currentUser.UserId.Value,
            Content = request.Content,
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);

        var approvalAction = new ApprovalAction
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.EnhancementRequestId,
            UserId = _currentUser.UserId.Value,
            ActionType = Domain.Enums.ApprovalActionType.AddComment,
            Comments = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ApprovalActions.Add(approvalAction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        approvalAction.User = user;
        return approvalAction.ToDto();
    }
}
