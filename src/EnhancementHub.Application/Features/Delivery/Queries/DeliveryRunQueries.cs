using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.Delivery.Queries;

public sealed record GetDeliveryRunQuery(Guid EnhancementRequestId) : IRequest<EnhancementDeliveryRunDto?>;

public sealed class GetDeliveryRunQueryHandler : IRequestHandler<GetDeliveryRunQuery, EnhancementDeliveryRunDto?>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly IFileStorageService _fileStorage;

    public GetDeliveryRunQueryHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService,
        IFileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _fileStorage = fileStorage;
    }

    public async Task<EnhancementDeliveryRunDto?> Handle(
        GetDeliveryRunQuery request,
        CancellationToken cancellationToken)
    {
        var enhancementRequest = await _accessService.GetAccessibleRequestAsync(
            request.EnhancementRequestId,
            cancellationToken);

        var run = await _dbContext.EnhancementDeliveryRuns
            .AsNoTracking()
            .Include(r => r.TestResults)
            .Where(r => r.EnhancementRequestId == request.EnhancementRequestId)
            .OrderByDescending(r => r.RunNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (run is null)
        {
            return null;
        }

        TenantDeliveryProfile? tenantProfile = null;
        ApplicationDeliveryProfile? appProfile = null;
        if (enhancementRequest.TargetApplicationId.HasValue)
        {
            appProfile = await _dbContext.ApplicationDeliveryProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationId == enhancementRequest.TargetApplicationId, cancellationToken);

            var tenantId = await (
                from app in _dbContext.Applications.AsNoTracking()
                join team in _dbContext.Teams.AsNoTracking() on app.OwnerTeamId equals team.Id
                where app.Id == enhancementRequest.TargetApplicationId
                select team.TenantId).FirstOrDefaultAsync(cancellationToken);

            if (tenantId != Guid.Empty)
            {
                tenantProfile = await _dbContext.TenantDeliveryProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
            }
        }

        var rollbackPlan = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => a.EnhancementRequestId == request.EnhancementRequestId)
            .OrderByDescending(a => a.Version)
            .Select(a => a.RollbackPlan)
            .FirstOrDefaultAsync(cancellationToken);

        return await DeliveryRunMapper.ToDtoAsync(
            run,
            _fileStorage,
            DeliveryRunGates.CanDeployToProduction(run, tenantProfile, appProfile),
            DeliveryRunGates.CanRollbackProduction(run, tenantProfile),
            appProfile?.RequiresHumanProdDeploy ?? false,
            rollbackPlan,
            cancellationToken);
    }
}
