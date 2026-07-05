using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record DetectSchemaDriftCommand(Guid ConnectionId, Guid? RepositoryId = null)
    : IRequest<DriftReportDto>;

public sealed class DetectSchemaDriftCommandHandler
    : IRequestHandler<DetectSchemaDriftCommand, DriftReportDto>
{
    private readonly ISchemaDriftDetector _detector;
    private readonly IMediator _mediator;
    private readonly IApplicationAccessService _accessService;

    public DetectSchemaDriftCommandHandler(
        ISchemaDriftDetector detector,
        IMediator mediator,
        IApplicationAccessService accessService)
    {
        _detector = detector;
        _mediator = mediator;
        _accessService = accessService;
    }

    public async Task<DriftReportDto> Handle(
        DetectSchemaDriftCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleConnectionAsync(request.ConnectionId, cancellationToken);

        await _detector.DetectDriftIfStaleAsync(request.ConnectionId, forceFullScan: false, cancellationToken);
        return await _mediator.Send(
            new Queries.GetDriftReportQuery(request.ConnectionId, request.RepositoryId),
            cancellationToken);
    }
}
