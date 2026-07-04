using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IOnPremAgentService
{
    Task<OnPremAgentRegistration> RegisterAgentAsync(string agentName, string? description, CancellationToken cancellationToken = default);
    Task AcceptScanPayloadAsync(Guid agentId, Guid databaseConnectionId, DatabaseSchemaScanResult scanResult, CancellationToken cancellationToken = default);
    IReadOnlyList<OnPremAgentRegistration> GetRegisteredAgents();
}
