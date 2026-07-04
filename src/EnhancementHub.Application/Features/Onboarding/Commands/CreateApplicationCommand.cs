using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed record CreateApplicationCommand(
    string Name,
    string? BusinessDomain,
    string? Purpose,
    string? Description,
    string? RiskSensitiveAreas,
    string? OwnerTeamName = null) : IRequest<ApplicationDto>;

public sealed class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BusinessDomain).MaximumLength(200);
        RuleFor(x => x.Purpose).MaximumLength(1000);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class CreateApplicationCommandHandler
    : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public CreateApplicationCommandHandler(IEnhancementHubDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var team = await ResolveOwnerTeamAsync(request.OwnerTeamName, cancellationToken);
        var now = DateTime.UtcNow;

        var entity = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            BusinessDomain = request.BusinessDomain?.Trim(),
            Purpose = request.Purpose?.Trim(),
            Description = request.Description?.Trim(),
            RiskSensitiveAreas = request.RiskSensitiveAreas?.Trim(),
            OwnerTeamId = team.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Applications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ApplicationCreated",
            nameof(ApplicationEntity),
            entity.Id,
            $"Created application '{entity.Name}'.",
            cancellationToken);

        entity.OwnerTeam = team;
        return new ApplicationDto(
            entity.Id,
            entity.Name,
            entity.BusinessDomain,
            entity.Purpose,
            entity.Description,
            entity.OwnerTeamId,
            entity.RiskSensitiveAreas,
            0);
    }

    private async Task<Team> ResolveOwnerTeamAsync(string? ownerTeamName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(ownerTeamName))
        {
            var existing = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.Name == ownerTeamName.Trim(), cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            var created = new Team
            {
                Id = Guid.NewGuid(),
                Name = ownerTeamName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Teams.Add(created);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }

        var team = await _dbContext.Teams.OrderBy(t => t.Name).FirstOrDefaultAsync(cancellationToken);
        if (team is not null)
        {
            return team;
        }

        var defaultTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Default Team",
            Description = "Auto-created during application onboarding",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Teams.Add(defaultTeam);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return defaultTeam;
    }
}
