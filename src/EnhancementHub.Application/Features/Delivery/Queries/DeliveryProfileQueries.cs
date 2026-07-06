using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Queries;

public sealed record GetTenantDeliveryProfileQuery(Guid? TenantId = null) : IRequest<TenantDeliveryProfileDto>;

public sealed class GetTenantDeliveryProfileQueryHandler
    : IRequestHandler<GetTenantDeliveryProfileQuery, TenantDeliveryProfileDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenant;

    public GetTenantDeliveryProfileQueryHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentTenantService currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<TenantDeliveryProfileDto> Handle(
        GetTenantDeliveryProfileQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _currentTenant.TenantId
            ?? throw new ForbiddenException("Tenant context is required.");

        var profile = await _dbContext.TenantDeliveryProfiles
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (profile is null)
        {
            var now = DateTime.UtcNow;
            profile = new TenantDeliveryProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedAt = now,
                UpdatedAt = now,
                RequirePullRequestReview = true,
                RequireUatSignoff = true,
                RequireProdChangeWindow = true,
            };
            _dbContext.TenantDeliveryProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return DeliveryProfileMapper.ToDto(profile);
    }
}

public sealed record GetApplicationDeliveryProfileQuery(Guid ApplicationId) : IRequest<ApplicationDeliveryProfileDto>;

public sealed class GetApplicationDeliveryProfileQueryHandler
    : IRequestHandler<GetApplicationDeliveryProfileQuery, ApplicationDeliveryProfileDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _applicationAccess;

    public GetApplicationDeliveryProfileQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService applicationAccess)
    {
        _dbContext = dbContext;
        _applicationAccess = applicationAccess;
    }

    public async Task<ApplicationDeliveryProfileDto> Handle(
        GetApplicationDeliveryProfileQuery request,
        CancellationToken cancellationToken)
    {
        await _applicationAccess.GetAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var profile = await _dbContext.ApplicationDeliveryProfiles
            .FirstOrDefaultAsync(p => p.ApplicationId == request.ApplicationId, cancellationToken);

        if (profile is null)
        {
            var now = DateTime.UtcNow;
            profile = new ApplicationDeliveryProfile
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _dbContext.ApplicationDeliveryProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var validation = DeliveryProfileValidator.ValidateApplicationProfile(profile);
        var messages = validation.Errors.Concat(validation.Warnings).ToList();
        return DeliveryProfileMapper.ToDto(profile, messages);
    }
}

public sealed record ValidateTenantDeliveryProfileQuery(Guid? TenantId = null)
    : IRequest<DeliveryProfileValidationResultDto>;

public sealed class ValidateTenantDeliveryProfileQueryHandler
    : IRequestHandler<ValidateTenantDeliveryProfileQuery, DeliveryProfileValidationResultDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenant;

    public ValidateTenantDeliveryProfileQueryHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentTenantService currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<DeliveryProfileValidationResultDto> Handle(
        ValidateTenantDeliveryProfileQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _currentTenant.TenantId
            ?? throw new ForbiddenException("Tenant context is required.");

        var profile = await _dbContext.TenantDeliveryProfiles
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var environments = profile?.Environments.ToList() ?? [];
        if (environments.Count == 0)
        {
            environments = await _dbContext.TenantDeploymentEnvironments
                .Where(e => e.TenantId == tenantId)
                .ToListAsync(cancellationToken);
        }

        return DeliveryProfileValidator.ValidateTenantProfile(profile, environments);
    }
}

public sealed record ValidateApplicationDeliveryProfileQuery(Guid ApplicationId)
    : IRequest<DeliveryProfileValidationResultDto>;

public sealed class ValidateApplicationDeliveryProfileQueryHandler
    : IRequestHandler<ValidateApplicationDeliveryProfileQuery, DeliveryProfileValidationResultDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _applicationAccess;

    public ValidateApplicationDeliveryProfileQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService applicationAccess)
    {
        _dbContext = dbContext;
        _applicationAccess = applicationAccess;
    }

    public async Task<DeliveryProfileValidationResultDto> Handle(
        ValidateApplicationDeliveryProfileQuery request,
        CancellationToken cancellationToken)
    {
        await _applicationAccess.GetAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var profile = await _dbContext.ApplicationDeliveryProfiles
            .FirstOrDefaultAsync(p => p.ApplicationId == request.ApplicationId, cancellationToken);

        if (profile is null)
        {
            return new DeliveryProfileValidationResultDto(
                false,
                ["Application delivery profile is not configured."],
                []);
        }

        return DeliveryProfileValidator.ValidateApplicationProfile(profile);
    }
}
