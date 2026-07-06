using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.CustomFields.Commands;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    string? SupportingNotes,
    Guid? TemplateId = null,
    IReadOnlyList<CustomFieldValueInput>? CustomFields = null) : IRequest<EnhancementRequestDto>;

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

        var title = request.Title;
        var businessDescription = request.BusinessDescription;
        var desiredOutcome = request.DesiredOutcome;
        var priority = request.Priority;
        var supportingNotes = request.SupportingNotes;

        if (request.TemplateId.HasValue)
        {
            var template = await _dbContext.EnhancementTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value && t.IsActive, cancellationToken)
                ?? throw new Common.Exceptions.NotFoundException("EnhancementTemplate", request.TemplateId.Value);

            title = string.IsNullOrWhiteSpace(title) ? template.Title : title;
            businessDescription = string.IsNullOrWhiteSpace(businessDescription)
                ? template.BusinessDescription
                : businessDescription;
            desiredOutcome = string.IsNullOrWhiteSpace(desiredOutcome)
                ? template.DesiredOutcome
                : desiredOutcome;
            priority = string.IsNullOrWhiteSpace(priority) ? template.Priority : priority;

            var templateNote = $"Template: {template.DomainCategory} | {template.Name}";
            if (!string.IsNullOrWhiteSpace(template.SupportingNotes))
            {
                supportingNotes = string.IsNullOrWhiteSpace(supportingNotes)
                    ? $"{templateNote}\n{template.SupportingNotes}"
                    : $"{templateNote}\n{supportingNotes}";
            }
            else
            {
                supportingNotes = string.IsNullOrWhiteSpace(supportingNotes)
                    ? templateNote
                    : $"{templateNote}\n{supportingNotes}";
            }
        }

        var entity = new EnhancementRequest
        {
            Id = Guid.NewGuid(),
            Title = title,
            BusinessDescription = businessDescription,
            DesiredOutcome = desiredOutcome,
            Priority = priority,
            TargetApplicationId = request.TargetApplicationId,
            RequestedDueDate = request.RequestedDueDate,
            SubmittedByUserId = _currentUser.UserId.Value,
            Department = request.Department,
            TeamId = request.TeamId,
            SupportingNotes = supportingNotes,
            Status = EnhancementRequestStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _dbContext.EnhancementRequests.Add(entity);
        await CustomFieldValueWriter.SaveValuesAsync(_dbContext, entity.Id, request.CustomFields, cancellationToken);
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
