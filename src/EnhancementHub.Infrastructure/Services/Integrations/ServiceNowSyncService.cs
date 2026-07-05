using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class ServiceNowSyncService : IServiceNowSyncService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IntegrationsOptions _options;

    public ServiceNowSyncService(
        IEnhancementHubDbContext dbContext,
        IOptions<IntegrationsOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<ServiceNowSyncResult> ApplyInboundUpdateAsync(
        ServiceNowInboundUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ServiceNow.Enabled)
        {
            return new ServiceNowSyncResult(false, null, "ServiceNow sync is disabled.");
        }

        var ticket = await _dbContext.ExternalTickets
            .FirstOrDefaultAsync(
                t => t.Provider == ExternalTicketProvider.ServiceNow && t.ExternalId == update.ExternalId,
                cancellationToken);

        if (ticket is null)
        {
            return new ServiceNowSyncResult(false, null, "No linked enhancement request found.");
        }

        var request = await _dbContext.EnhancementRequests
            .FirstOrDefaultAsync(r => r.Id == ticket.EnhancementRequestId, cancellationToken);

        if (request is null)
        {
            return new ServiceNowSyncResult(false, null, "Enhancement request not found.");
        }

        var mappedStatus = MapState(update.State);
        if (mappedStatus.HasValue && request.Status != mappedStatus.Value)
        {
            request.Status = mappedStatus.Value;
            request.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ServiceNowSyncResult(true, request.Id, "Status synchronized.");
    }

    internal static EnhancementRequestStatus? MapState(string state) =>
        state.ToLowerInvariant() switch
        {
            "approved" or "scheduled" or "implement" => EnhancementRequestStatus.Approved,
            "closed" or "complete" => EnhancementRequestStatus.ReadyForDevelopment,
            "cancelled" or "canceled" => EnhancementRequestStatus.Rejected,
            _ => null
        };
}
