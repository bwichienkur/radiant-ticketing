using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid TeamMemberId) : IRequest<bool>;

public sealed class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.TeamMemberId).NotEmpty();
    }
}

public sealed class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand, bool>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RemoveTeamMemberCommandHandler(IEnhancementHubDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<bool> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var membership = await _dbContext.TeamMembers
            .Include(m => m.User)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(
                m => m.Id == request.TeamMemberId && m.TeamId == request.TeamId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(TeamMember), request.TeamMemberId);

        _dbContext.TeamMembers.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "TeamMemberRemoved",
            nameof(TeamMember),
            membership.Id,
            $"Removed user '{membership.User.Email}' from team '{membership.Team.Name}'.",
            cancellationToken);

        return true;
    }
}
