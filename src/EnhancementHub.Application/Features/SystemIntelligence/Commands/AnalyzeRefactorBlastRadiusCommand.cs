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
    private readonly IApplicationAccessService _accessService;

    public AnalyzeRefactorBlastRadiusCommandHandler(
        IRefactorBlastRadiusService blastRadiusService,
        IApplicationAccessService accessService)
    {
        _blastRadiusService = blastRadiusService;
        _accessService = accessService;
    }

    public async Task<BlastRadiusResultDto> Handle(
        AnalyzeRefactorBlastRadiusCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var result = await _blastRadiusService.AnalyzeAsync(
            request.ApplicationId,
            request.Target,
            cancellationToken);

        return new BlastRadiusResultDto(
            result.TargetName,
            BlastRadiusMapper.ToDto(result).AffectedItems);
    }
}
