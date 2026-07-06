using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using FluentValidation;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Commands;

public sealed record UpdateTenantDeliveryProfileCommand(
    CicdProvider DefaultCicdProvider,
    string? VaultSecretPrefix,
    bool AutoImplementOnApprove,
    bool AutoDeployToTest,
    bool RequirePullRequestReview,
    bool RequireUatSignoff,
    bool RequireProdChangeWindow,
    string? ChangeWindowNotes,
    int QaVideoRetentionDays,
    Guid? TenantId = null) : IRequest<TenantDeliveryProfileDto>;

public sealed class UpdateTenantDeliveryProfileCommandValidator : AbstractValidator<UpdateTenantDeliveryProfileCommand>
{
    public UpdateTenantDeliveryProfileCommandValidator()
    {
        RuleFor(x => x.VaultSecretPrefix).MaximumLength(500);
        RuleFor(x => x.ChangeWindowNotes).MaximumLength(4000);
        RuleFor(x => x.QaVideoRetentionDays).InclusiveBetween(7, 3650);
    }
}

public sealed class UpdateTenantDeliveryProfileCommandHandler
    : IRequestHandler<UpdateTenantDeliveryProfileCommand, TenantDeliveryProfileDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IAuditService _auditService;

    public UpdateTenantDeliveryProfileCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentTenantService currentTenant,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditService = auditService;
    }

    public async Task<TenantDeliveryProfileDto> Handle(
        UpdateTenantDeliveryProfileCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _currentTenant.TenantId
            ?? throw new ForbiddenException("Tenant context is required.");

        var profile = await _dbContext.TenantDeliveryProfiles
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new TenantDeliveryProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedAt = now,
            };
            _dbContext.TenantDeliveryProfiles.Add(profile);
        }

        profile.DefaultCicdProvider = request.DefaultCicdProvider;
        profile.VaultSecretPrefix = request.VaultSecretPrefix?.Trim();
        profile.AutoImplementOnApprove = request.AutoImplementOnApprove;
        profile.AutoDeployToTest = request.AutoDeployToTest;
        profile.RequirePullRequestReview = request.RequirePullRequestReview;
        profile.RequireUatSignoff = request.RequireUatSignoff;
        profile.RequireProdChangeWindow = request.RequireProdChangeWindow;
        profile.ChangeWindowNotes = request.ChangeWindowNotes?.Trim();
        profile.QaVideoRetentionDays = request.QaVideoRetentionDays;
        profile.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "TenantDeliveryProfileUpdated",
            nameof(TenantDeliveryProfile),
            profile.Id,
            "Updated tenant delivery profile.",
            cancellationToken);

        return DeliveryProfileMapper.ToDto(profile);
    }
}

public sealed record UpsertTenantDeploymentEnvironmentCommand(
    Guid? EnvironmentId,
    string Name,
    DeploymentEnvironmentType EnvironmentType,
    string? BaseUrlTemplate,
    string? SecretReferencePrefix,
    bool IsActive,
    int SortOrder,
    bool RequiresApprovalForDeploy,
    Guid? TenantId = null) : IRequest<TenantDeploymentEnvironmentDto>;

public sealed class UpsertTenantDeploymentEnvironmentCommandValidator
    : AbstractValidator<UpsertTenantDeploymentEnvironmentCommand>
{
    public UpsertTenantDeploymentEnvironmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BaseUrlTemplate).MaximumLength(500);
        RuleFor(x => x.SecretReferencePrefix).MaximumLength(500);
    }
}

public sealed class UpsertTenantDeploymentEnvironmentCommandHandler
    : IRequestHandler<UpsertTenantDeploymentEnvironmentCommand, TenantDeploymentEnvironmentDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IAuditService _auditService;

    public UpsertTenantDeploymentEnvironmentCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentTenantService currentTenant,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditService = auditService;
    }

    public async Task<TenantDeploymentEnvironmentDto> Handle(
        UpsertTenantDeploymentEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _currentTenant.TenantId
            ?? throw new ForbiddenException("Tenant context is required.");

        TenantDeploymentEnvironment entity;
        var now = DateTime.UtcNow;

        if (request.EnvironmentId.HasValue)
        {
            entity = await _dbContext.TenantDeploymentEnvironments
                .FirstOrDefaultAsync(
                    e => e.Id == request.EnvironmentId.Value && e.TenantId == tenantId,
                    cancellationToken)
                ?? throw new NotFoundException("Deployment environment not found.");
        }
        else
        {
            entity = new TenantDeploymentEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedAt = now,
            };
            _dbContext.TenantDeploymentEnvironments.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.EnvironmentType = request.EnvironmentType;
        entity.BaseUrlTemplate = request.BaseUrlTemplate?.Trim();
        entity.SecretReferencePrefix = request.SecretReferencePrefix?.Trim();
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.RequiresApprovalForDeploy = request.RequiresApprovalForDeploy;
        entity.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            request.EnvironmentId.HasValue ? "TenantDeploymentEnvironmentUpdated" : "TenantDeploymentEnvironmentCreated",
            nameof(TenantDeploymentEnvironment),
            entity.Id,
            $"Environment '{entity.Name}' ({entity.EnvironmentType}).",
            cancellationToken);

        return new TenantDeploymentEnvironmentDto(
            entity.Id,
            entity.Name,
            entity.EnvironmentType,
            entity.BaseUrlTemplate,
            entity.SecretReferencePrefix,
            entity.IsActive,
            entity.SortOrder,
            entity.RequiresApprovalForDeploy);
    }
}

public sealed record DeleteTenantDeploymentEnvironmentCommand(Guid EnvironmentId, Guid? TenantId = null)
    : IRequest<Unit>;

public sealed class DeleteTenantDeploymentEnvironmentCommandHandler
    : IRequestHandler<DeleteTenantDeploymentEnvironmentCommand, Unit>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenant;

    public DeleteTenantDeploymentEnvironmentCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentTenantService currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async Task<Unit> Handle(
        DeleteTenantDeploymentEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? _currentTenant.TenantId
            ?? throw new ForbiddenException("Tenant context is required.");

        var entity = await _dbContext.TenantDeploymentEnvironments
            .FirstOrDefaultAsync(
                e => e.Id == request.EnvironmentId && e.TenantId == tenantId,
                cancellationToken)
            ?? throw new NotFoundException("Deployment environment not found.");

        _dbContext.TenantDeploymentEnvironments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed record UpdateApplicationDeliveryProfileCommand(
    Guid ApplicationId,
    DeploymentMechanism DeploymentMechanism,
    Guid? PrimaryRepositoryId,
    string BranchNamingPattern,
    string? CicdPipelineReference,
    CicdProvider? CicdProviderOverride,
    string SmokeTestPath,
    DatabaseMigrationStrategy DatabaseMigrationStrategy,
    bool RequiresHumanProdDeploy,
    string? ConfigTransformsJson,
    string? ConnectionMappingsJson) : IRequest<ApplicationDeliveryProfileDto>;

public sealed class UpdateApplicationDeliveryProfileCommandValidator
    : AbstractValidator<UpdateApplicationDeliveryProfileCommand>
{
    public UpdateApplicationDeliveryProfileCommandValidator()
    {
        RuleFor(x => x.BranchNamingPattern).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CicdPipelineReference).MaximumLength(500);
        RuleFor(x => x.SmokeTestPath).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateApplicationDeliveryProfileCommandHandler
    : IRequestHandler<UpdateApplicationDeliveryProfileCommand, ApplicationDeliveryProfileDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _applicationAccess;
    private readonly IAuditService _auditService;

    public UpdateApplicationDeliveryProfileCommandHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService applicationAccess,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _applicationAccess = applicationAccess;
        _auditService = auditService;
    }

    public async Task<ApplicationDeliveryProfileDto> Handle(
        UpdateApplicationDeliveryProfileCommand request,
        CancellationToken cancellationToken)
    {
        await _applicationAccess.GetAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        if (request.PrimaryRepositoryId.HasValue)
        {
            var repoExists = await _dbContext.Repositories.AnyAsync(
                r => r.Id == request.PrimaryRepositoryId.Value && r.ApplicationId == request.ApplicationId,
                cancellationToken);
            if (!repoExists)
            {
                throw new ValidationException("Primary repository must belong to this application.");
            }
        }

        var profile = await _dbContext.ApplicationDeliveryProfiles
            .FirstOrDefaultAsync(p => p.ApplicationId == request.ApplicationId, cancellationToken);

        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new ApplicationDeliveryProfile
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                CreatedAt = now,
            };
            _dbContext.ApplicationDeliveryProfiles.Add(profile);
        }

        profile.DeploymentMechanism = request.DeploymentMechanism;
        profile.PrimaryRepositoryId = request.PrimaryRepositoryId;
        profile.BranchNamingPattern = request.BranchNamingPattern.Trim();
        profile.CicdPipelineReference = request.CicdPipelineReference?.Trim();
        profile.CicdProviderOverride = request.CicdProviderOverride;
        profile.SmokeTestPath = request.SmokeTestPath.Trim();
        profile.DatabaseMigrationStrategy = request.DatabaseMigrationStrategy;
        profile.RequiresHumanProdDeploy = request.RequiresHumanProdDeploy;
        profile.ConfigTransformsJson = request.ConfigTransformsJson?.Trim();
        profile.ConnectionMappingsJson = request.ConnectionMappingsJson?.Trim();
        profile.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ApplicationDeliveryProfileUpdated",
            nameof(ApplicationDeliveryProfile),
            profile.Id,
            $"Updated delivery profile for application {request.ApplicationId}.",
            cancellationToken);

        var validation = DeliveryProfileValidator.ValidateApplicationProfile(profile);
        var messages = validation.Errors.Concat(validation.Warnings).ToList();
        return DeliveryProfileMapper.ToDto(profile, messages);
    }
}
