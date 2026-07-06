using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Feedback.Dtos;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Feedback.Commands;

public sealed record SubmitProductFeedbackCommand(
    string WorkflowKey,
    int NpsScore,
    string? Comment) : IRequest<ProductFeedbackDto>;

public sealed class SubmitProductFeedbackCommandValidator : AbstractValidator<SubmitProductFeedbackCommand>
{
    public SubmitProductFeedbackCommandValidator()
    {
        RuleFor(x => x.WorkflowKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NpsScore).InclusiveBetween(0, 10);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public sealed class SubmitProductFeedbackCommandHandler
    : IRequestHandler<SubmitProductFeedbackCommand, ProductFeedbackDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public SubmitProductFeedbackCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<ProductFeedbackDto> Handle(
        SubmitProductFeedbackCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException("User must be authenticated to submit feedback.");
        }

        var tenantId = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowKey = request.WorkflowKey.Trim(),
            NpsScore = request.NpsScore,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _dbContext.ProductFeedbacks.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductFeedbackDto(
            feedback.Id,
            feedback.WorkflowKey,
            feedback.NpsScore,
            feedback.Comment,
            feedback.CreatedAt);
    }
}
