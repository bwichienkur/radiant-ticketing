using System.Data.Common;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class TenantIsolationService : ITenantIsolationService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly TenantIsolationOptions _options;
    private readonly ILogger<TenantIsolationService> _logger;

    public TenantIsolationService(
        IEnhancementHubDbContext dbContext,
        IOptions<TenantIsolationOptions> options,
        ILogger<TenantIsolationService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TenantIsolationStatus> GetIsolationStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        return MapStatus(tenant);
    }

    public async Task<TenantIsolationStatus> ProvisionDedicatedSchemaAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new ForbiddenException("Tenant schema isolation is disabled.");
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        var schemaName = tenant.DatabaseSchemaName
            ?? TenantSchemaNameResolver.BuildSchemaName(tenant.Slug, _options);

        if (!TenantSchemaNameResolver.IsValidSchemaName(schemaName))
        {
            throw new ForbiddenException("Unable to derive a valid database schema name for this tenant.");
        }

        await TenantSchemaProvisioner.ProvisionAsync(
            _dbContext,
            schemaName,
            _options.ControlPlaneTables,
            _logger,
            cancellationToken);

        tenant.IsolationMode = TenantIsolationMode.DedicatedSchema;
        tenant.DatabaseSchemaName = schemaName;
        tenant.SchemaProvisionedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned dedicated schema {Schema} for tenant {TenantId}",
            schemaName,
            tenantId);

        return MapStatus(tenant);
    }

    public async Task TryAutoProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null || tenant.SchemaProvisionedAt.HasValue)
        {
            return;
        }

        var shouldProvision = (_options.AutoProvisionEnterprise && tenant.Plan == TenantPlan.Enterprise)
                              || (_options.AutoProvisionEuRegion && tenant.Region == TenantRegion.EU);

        if (!shouldProvision)
        {
            return;
        }

        await ProvisionDedicatedSchemaAsync(tenantId, cancellationToken);
    }

    private TenantIsolationStatus MapStatus(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.IsolationMode.ToString(),
            tenant.DatabaseSchemaName,
            tenant.SchemaProvisionedAt.HasValue,
            tenant.SchemaProvisionedAt,
            _options.Enabled);
}
