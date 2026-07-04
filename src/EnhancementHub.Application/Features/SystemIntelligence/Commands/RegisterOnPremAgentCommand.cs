using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record RegisterOnPremAgentCommand(
    string Name,
    string? Description = null) : IRequest<OnPremAgentDto>;

public sealed class RegisterOnPremAgentCommandHandler
    : IRequestHandler<RegisterOnPremAgentCommand, OnPremAgentDto>
{
    private readonly IOnPremAgentService _agentService;

    public RegisterOnPremAgentCommandHandler(IOnPremAgentService agentService) =>
        _agentService = agentService;

    public async Task<OnPremAgentDto> Handle(
        RegisterOnPremAgentCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _agentService.RegisterAgentAsync(
            request.Name,
            request.Description,
            cancellationToken);

        return new OnPremAgentDto(
            registration.AgentId,
            registration.AgentName,
            null,
            registration.LastSeenAt,
            true,
            registration.AgentId.ToString("N"));
    }
}
