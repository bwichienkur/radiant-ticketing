using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record BuildSystemGraphCommand(Guid ApplicationId) : IRequest<SystemMapDto>;

public sealed class BuildSystemGraphCommandHandler
    : IRequestHandler<BuildSystemGraphCommand, SystemMapDto>
{
    private readonly ISystemGraphBuilder _graphBuilder;
    private readonly IMediator _mediator;
    private readonly IApplicationAccessService _accessService;

    public BuildSystemGraphCommandHandler(
        ISystemGraphBuilder graphBuilder,
        IMediator mediator,
        IApplicationAccessService accessService)
    {
        _graphBuilder = graphBuilder;
        _mediator = mediator;
        _accessService = accessService;
    }

    public async Task<SystemMapDto> Handle(
        BuildSystemGraphCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        await _graphBuilder.BuildForApplicationAsync(request.ApplicationId, cancellationToken);
        return await _mediator.Send(new Queries.GetSystemMapQuery(request.ApplicationId), cancellationToken);
    }
}
