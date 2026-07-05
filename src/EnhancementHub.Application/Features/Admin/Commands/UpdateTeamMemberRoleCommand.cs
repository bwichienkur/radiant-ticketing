using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record UpdateTeamMemberRoleCommand(
    Guid TeamId,
    Guid TeamMemberId,
    string TeamRole) : IRequest<bool>;

public sealed class UpdateTeamMemberRoleCommandValidator : AbstractValidator<UpdateTeamMemberRoleCommand>
{
    public UpdateTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.TeamRole)
            .NotEmpty()
            .Must(TeamMemberRoles.All.Contains)
            .WithMessage($"Team role must be one of: {string.Join(", ", TeamMemberRoles.All)}.");
    }
}

public sealed class UpdateTeamMemberRoleCommandHandler : IRequestHandler<UpdateTeamMemberRoleCommand, bool>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public UpdateTeamMemberRoleCommandHandler(IEnhancementHubDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<bool> Handle(UpdateTeamMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var membership = await _dbContext.TeamMembers
            .Include(m => m.User)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(
                m => m.Id == request.TeamMemberId && m.TeamId == request.TeamId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(TeamMember), request.TeamMemberId);

        var previousRole = membership.Role;
        membership.Role = request.TeamRole;
        membership.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "TeamMemberRoleUpdated",
            nameof(TeamMember),
            membership.Id,
            $"Updated team role for '{membership.User.Email}' in '{membership.Team.Name}' from {previousRole} to {request.TeamRole}.",
            cancellationToken);

        return true;
    }
}
