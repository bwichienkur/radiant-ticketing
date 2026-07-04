using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record GenerateRefactorPlanCommand(
    Guid ApplicationId,
    string Target) : IRequest<RefactorPlanDetailDto>;

public sealed class GenerateRefactorPlanCommandHandler
    : IRequestHandler<GenerateRefactorPlanCommand, RefactorPlanDetailDto>
{
    private readonly IRefactorPlanGenerator _planGenerator;
    private readonly IRefactorBlastRadiusService _blastRadiusService;
    private readonly IEnhancementHubDbContext _dbContext;

    public GenerateRefactorPlanCommandHandler(
        IRefactorPlanGenerator planGenerator,
        IRefactorBlastRadiusService blastRadiusService,
        IEnhancementHubDbContext dbContext)
    {
        _planGenerator = planGenerator;
        _blastRadiusService = blastRadiusService;
        _dbContext = dbContext;
    }

    public async Task<RefactorPlanDetailDto> Handle(
        GenerateRefactorPlanCommand request,
        CancellationToken cancellationToken)
    {
        var applicationExists = await _dbContext.Applications
            .AnyAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (!applicationExists)
        {
            throw new NotFoundException(nameof(ApplicationEntity), request.ApplicationId);
        }

        var connection = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationId == request.ApplicationId, cancellationToken);

        var repository = await _dbContext.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ApplicationId == request.ApplicationId, cancellationToken);

        var blastRadius = await _blastRadiusService.AnalyzeAsync(
            request.ApplicationId,
            request.Target,
            cancellationToken);

        var plan = await _planGenerator.GenerateAsync(
            request.Target,
            enhancementRequestId: null,
            databaseConnectionId: connection?.Id,
            repositoryId: repository?.Id,
            blastRadius,
            cancellationToken);

        var migrationMarkdown = string.Join(
            "\n",
            plan.MigrationSteps.OrderBy(s => s.Order).Select(s =>
                $"### Step {s.Order}: {s.Description}\n```sql\n{s.SqlScript}\n```"));

        var now = DateTime.UtcNow;
        var entity = new RefactorPlan
        {
            Id = Guid.NewGuid(),
            DatabaseConnectionId = connection?.Id,
            RepositoryId = repository?.Id,
            Title = plan.Title,
            TargetDescription = plan.TargetDescription,
            BlastRadiusJson = JsonSerializer.Serialize(blastRadius),
            MigrationStepsJson = migrationMarkdown,
            RiskLevel = plan.RiskLevel,
            ConfidenceScore = plan.ConfidenceScore,
            Status = RefactorPlanStatus.Draft,
            GeneratedByAi = plan.GeneratedByAi,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.RefactorPlans.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefactorPlanDetailDto(
            entity.Id,
            entity.Title,
            entity.TargetDescription,
            migrationMarkdown,
            BlastRadiusMapper.ToDto(blastRadius),
            entity.Status,
            entity.CreatedAt);
    }
}
