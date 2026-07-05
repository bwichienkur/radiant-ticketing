using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Middleware;

public sealed class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantIsolationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenantService currentTenant,
        ITenantSchemaAccessor schemaAccessor,
        IEnhancementHubDbContext dbContext)
    {
        if (currentTenant.TenantId.HasValue && dbContext is DbContext)
        {
            var tenant = await dbContext.Tenants
                .AsNoTracking()
                .Where(t => t.Id == currentTenant.TenantId.Value)
                .Select(t => new { t.IsolationMode, t.DatabaseSchemaName })
                .FirstOrDefaultAsync(context.RequestAborted);

            if (tenant?.IsolationMode == TenantIsolationMode.DedicatedSchema
                && !string.IsNullOrWhiteSpace(tenant.DatabaseSchemaName))
            {
                schemaAccessor.ActiveSchemaName = tenant.DatabaseSchemaName;
            }
        }

        await _next(context);
    }
}

public static class TenantIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIsolation(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantIsolationMiddleware>();
}
