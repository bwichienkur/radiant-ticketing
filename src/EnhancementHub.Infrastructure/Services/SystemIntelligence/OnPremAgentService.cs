using System.Security.Cryptography;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class OnPremAgentService : IOnPremAgentService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly DatabaseSchemaIngestionService _ingestionService;

    public OnPremAgentService(
        IEnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher,
        DatabaseSchemaIngestionService ingestionService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _ingestionService = ingestionService;
    }

    public async Task<OnPremAgentRegistration> RegisterAgentAsync(
        string agentName,
        string? description,
        Guid? applicationId = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = GenerateApiKey();
        var now = DateTime.UtcNow;

        var agent = new OnPremAgent
        {
            Id = Guid.NewGuid(),
            Name = agentName.Trim(),
            ApiKeyHash = _passwordHasher.Hash(apiKey),
            ApplicationId = applicationId,
            IsActive = true,
            LastSeenAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.OnPremAgents.Add(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OnPremAgentRegistration
        {
            AgentId = agent.Id,
            AgentName = agent.Name,
            Description = description,
            ApplicationId = applicationId,
            RegisteredAt = now,
            LastSeenAt = now,
            ApiKey = apiKey
        };
    }

    public async Task AcceptScanPayloadAsync(
        Guid agentId,
        string apiKey,
        Guid databaseConnectionId,
        DatabaseSchemaScanResult scanResult,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new UnauthorizedAccessException("Agent API key is required.");
        }

        var agent = await _dbContext.OnPremAgents
            .FirstOrDefaultAsync(a => a.Id == agentId && a.IsActive, cancellationToken);

        if (agent is null || !_passwordHasher.Verify(apiKey, agent.ApiKeyHash))
        {
            throw new UnauthorizedAccessException("Invalid on-prem agent credentials.");
        }

        agent.LastSeenAt = DateTime.UtcNow;
        agent.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _ingestionService.IngestScanResultAsync(databaseConnectionId, scanResult, cancellationToken);
    }

    public async Task<IReadOnlyList<OnPremAgentRegistration>> GetRegisteredAgentsAsync(
        CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.OnPremAgents
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return agents.Select(a => new OnPremAgentRegistration
        {
            AgentId = a.Id,
            AgentName = a.Name,
            ApplicationId = a.ApplicationId,
            RegisteredAt = a.CreatedAt,
            LastSeenAt = a.LastSeenAt
        }).ToList();
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
