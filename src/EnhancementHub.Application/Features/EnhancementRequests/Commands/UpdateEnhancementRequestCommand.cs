using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Commands;

public sealed record UpdateEnhancementRequestCommand(
    Guid Id,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    DateTime? RequestedDueDate,
    string? Department,
    Guid? TeamId,
    string? SupportingNotes) : IRequest<EnhancementRequestDto>;

public sealed class UpdateEnhancementRequestCommandHandler
    : IRequestHandler<UpdateEnhancementRequestCommand, EnhancementRequestDto>
{
    private static readonly EnhancementRequestStatus[] EditableStatuses =
    [
        EnhancementRequestStatus.Submitted,
        EnhancementRequestStatus.NeedsClarification,
        EnhancementRequestStatus.PendingApproval
    ];

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;

    public UpdateEnhancementRequestCommandHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<EnhancementRequestDto> Handle(
        UpdateEnhancementRequestCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureCanModifyAsync(request.Id, cancellationToken);

        var entity = await _dbContext.EnhancementRequests
            .Include(r => r.TargetApplication)
            .Include(r => r.SubmittedByUser)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.Id);

        if (!EditableStatuses.Contains(entity.Status))
        {
            throw new ForbiddenException(
                $"Enhancement request cannot be updated while in status '{entity.Status}'.");
        }

        entity.Title = request.Title;
        entity.BusinessDescription = request.BusinessDescription;
        entity.DesiredOutcome = request.DesiredOutcome;
        entity.Priority = request.Priority;
        entity.TargetApplicationId = request.TargetApplicationId;
        entity.RequestedDueDate = request.RequestedDueDate;
        entity.Department = request.Department;
        entity.TeamId = request.TeamId;
        entity.SupportingNotes = request.SupportingNotes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
