using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IOnPremAgentService
{
    Task<OnPremAgentRegistration> RegisterAgentAsync(
        string agentName,
        string? description,
        Guid? applicationId = null,
        CancellationToken cancellationToken = default);

    Task AcceptScanPayloadAsync(
        Guid agentId,
        string apiKey,
        Guid databaseConnectionId,
        DatabaseSchemaScanResult scanResult,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OnPremAgentRegistration>> GetRegisteredAgentsAsync(
        CancellationToken cancellationToken = default);
}
