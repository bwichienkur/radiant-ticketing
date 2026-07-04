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

    public DetectSchemaDriftCommandHandler(ISchemaDriftDetector detector, IMediator mediator)
    {
        _detector = detector;
        _mediator = mediator;
    }

    public async Task<DriftReportDto> Handle(
        DetectSchemaDriftCommand request,
        CancellationToken cancellationToken)
    {
        await _detector.DetectDriftAsync(request.ConnectionId, cancellationToken);
        return await _mediator.Send(
            new Queries.GetDriftReportQuery(request.ConnectionId, request.RepositoryId),
            cancellationToken);
    }
}
