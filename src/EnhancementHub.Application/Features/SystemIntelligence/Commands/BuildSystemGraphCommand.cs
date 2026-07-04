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

    public BuildSystemGraphCommandHandler(ISystemGraphBuilder graphBuilder, IMediator mediator)
    {
        _graphBuilder = graphBuilder;
        _mediator = mediator;
    }

    public async Task<SystemMapDto> Handle(
        BuildSystemGraphCommand request,
        CancellationToken cancellationToken)
    {
        await _graphBuilder.BuildForApplicationAsync(request.ApplicationId, cancellationToken);
        return await _mediator.Send(new Queries.GetSystemMapQuery(request.ApplicationId), cancellationToken);
    }
}
