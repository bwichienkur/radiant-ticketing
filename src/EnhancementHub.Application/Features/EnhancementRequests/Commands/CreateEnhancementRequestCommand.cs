using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;

namespace EnhancementHub.Application.Features.EnhancementRequests.Commands;

public sealed record CreateEnhancementRequestCommand(
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    DateTime? RequestedDueDate,
    string? Department,
    Guid? TeamId,
    string? SupportingNotes) : IRequest<EnhancementRequestDto>;

public sealed class CreateEnhancementRequestCommandValidator : AbstractValidator<CreateEnhancementRequestCommand>
{
    public CreateEnhancementRequestCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BusinessDescription).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.DesiredOutcome).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Priority).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateEnhancementRequestCommandHandler
    : IRequestHandler<CreateEnhancementRequestCommand, EnhancementRequestDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public CreateEnhancementRequestCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<EnhancementRequestDto> Handle(
        CreateEnhancementRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create enhancement requests.");
        }

        var entity = new EnhancementRequest
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            BusinessDescription = request.BusinessDescription,
            DesiredOutcome = request.DesiredOutcome,
            Priority = request.Priority,
            TargetApplicationId = request.TargetApplicationId,
            RequestedDueDate = request.RequestedDueDate,
            SubmittedByUserId = _currentUser.UserId.Value,
            Department = request.Department,
            TeamId = request.TeamId,
            SupportingNotes = request.SupportingNotes,
            Status = EnhancementRequestStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _dbContext.EnhancementRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new EnhancementRequestDto(
            entity.Id,
            entity.Title,
            entity.BusinessDescription,
            entity.DesiredOutcome,
            entity.Priority,
            entity.TargetApplicationId,
            null,
            entity.RequestedDueDate,
            entity.SubmittedByUserId,
            _currentUser.DisplayName,
            entity.Department,
            entity.TeamId,
            entity.Status,
            entity.SupportingNotes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
