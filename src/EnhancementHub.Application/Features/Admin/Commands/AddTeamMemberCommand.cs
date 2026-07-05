using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record AddTeamMemberCommand(
    Guid TeamId,
    string Email,
    string DisplayName,
    UserRole GlobalRole,
    string TeamRole) : IRequest<AddTeamMemberResultDto>;

public sealed class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GlobalRole).IsInEnum();
        RuleFor(x => x.TeamRole)
            .NotEmpty()
            .Must(TeamMemberRoles.All.Contains)
            .WithMessage($"Team role must be one of: {string.Join(", ", TeamMemberRoles.All)}.");
    }
}

public sealed class AddTeamMemberCommandHandler : IRequestHandler<AddTeamMemberCommand, AddTeamMemberResultDto>
{
    public const string DefaultInvitePassword = "password123";

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public AddTeamMemberCommandHandler(
        IEnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<AddTeamMemberResultDto> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.TeamId);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        var userCreated = false;
        string? temporaryPassword = null;

        if (user is null)
        {
            var now = DateTime.UtcNow;
            temporaryPassword = DefaultInvitePassword;
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = request.DisplayName.Trim(),
                Role = request.GlobalRole,
                IsActive = true,
                PasswordHash = _passwordHasher.Hash(DefaultInvitePassword),
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.Users.Add(user);
            userCreated = true;
        }
        else
        {
            user.DisplayName = request.DisplayName.Trim();
            user.Role = request.GlobalRole;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
        }

        var existingMembership = await _dbContext.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == team.Id && m.UserId == user.Id, cancellationToken);

        if (existingMembership is not null)
        {
            throw new ValidationException($"User '{email}' is already a member of team '{team.Name}'.");
        }

        var nowMember = DateTime.UtcNow;
        var membership = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = user.Id,
            Role = request.TeamRole,
            CreatedAt = nowMember,
            UpdatedAt = nowMember
        };

        _dbContext.TeamMembers.Add(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userCreated ? "TeamMemberInvited" : "TeamMemberAdded",
            nameof(TeamMember),
            membership.Id,
            $"{(userCreated ? "Invited" : "Added")} user '{user.Email}' to team '{team.Name}' as {request.TeamRole} (global role: {request.GlobalRole}).",
            cancellationToken);

        return new AddTeamMemberResultDto(
            membership.Id,
            user.Id,
            user.Email,
            userCreated,
            temporaryPassword);
    }
}
