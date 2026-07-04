using System.Collections.Concurrent;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class OnPremAgentService : IOnPremAgentService
{
    private readonly DatabaseSchemaIngestionService _ingestionService;
    private readonly ConcurrentDictionary<Guid, OnPremAgentRegistration> _agents = new();

    public OnPremAgentService(DatabaseSchemaIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    public Task<OnPremAgentRegistration> RegisterAgentAsync(
        string agentName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var registration = new OnPremAgentRegistration
        {
            AgentId = Guid.NewGuid(),
            AgentName = agentName,
            Description = description,
            RegisteredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _agents[registration.AgentId] = registration;
        return Task.FromResult(registration);
    }

    public async Task AcceptScanPayloadAsync(
        Guid agentId,
        Guid databaseConnectionId,
        DatabaseSchemaScanResult scanResult,
        CancellationToken cancellationToken = default)
    {
        if (!_agents.ContainsKey(agentId))
        {
            throw new InvalidOperationException($"On-prem agent {agentId} is not registered.");
        }

        _agents[agentId] = new OnPremAgentRegistration
        {
            AgentId = _agents[agentId].AgentId,
            AgentName = _agents[agentId].AgentName,
            Description = _agents[agentId].Description,
            RegisteredAt = _agents[agentId].RegisteredAt,
            LastSeenAt = DateTime.UtcNow
        };
        await _ingestionService.IngestScanResultAsync(databaseConnectionId, scanResult, cancellationToken);
    }

    public IReadOnlyList<OnPremAgentRegistration> GetRegisteredAgents() =>
        _agents.Values.OrderBy(a => a.AgentName).ToList();
}
