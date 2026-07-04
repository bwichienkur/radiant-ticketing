using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record AnalyzeRefactorBlastRadiusCommand(
    Guid ApplicationId,
    string Target) : IRequest<BlastRadiusResultDto>;

public sealed class AnalyzeRefactorBlastRadiusCommandHandler
    : IRequestHandler<AnalyzeRefactorBlastRadiusCommand, BlastRadiusResultDto>
{
    private readonly IRefactorBlastRadiusService _blastRadiusService;

    public AnalyzeRefactorBlastRadiusCommandHandler(IRefactorBlastRadiusService blastRadiusService) =>
        _blastRadiusService = blastRadiusService;

    public async Task<BlastRadiusResultDto> Handle(
        AnalyzeRefactorBlastRadiusCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _blastRadiusService.AnalyzeAsync(
            request.ApplicationId,
            request.Target,
            cancellationToken);

        return new BlastRadiusResultDto(
            result.TargetName,
            BlastRadiusMapper.ToDto(result).AffectedItems);
    }
}
