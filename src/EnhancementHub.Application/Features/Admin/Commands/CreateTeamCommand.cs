using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record CreateTeamCommand(string Name, string? Description) : IRequest<TeamSummaryDto>;

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, TeamSummaryDto>
{
    private readonly ITeamRepository _teams;
    private readonly IAuditService _auditService;

    public CreateTeamCommandHandler(ITeamRepository teams, IAuditService auditService)
    {
        _teams = teams;
        _auditService = auditService;
    }

    public async Task<TeamSummaryDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _teams.NameExistsAsync(name, cancellationToken))
        {
            throw new ValidationException($"A team named '{name}' already exists.");
        }

        var now = DateTime.UtcNow;
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _teams.Add(team);
        await _teams.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "TeamCreated",
            nameof(Team),
            team.Id,
            $"Created team '{team.Name}'.",
            cancellationToken);

        return new TeamSummaryDto(team.Id, team.Name, team.Description, 0, 0);
    }
}
