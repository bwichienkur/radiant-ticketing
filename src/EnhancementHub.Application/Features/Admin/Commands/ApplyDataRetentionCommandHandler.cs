using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed class ApplyDataRetentionCommandHandler
    : IRequestHandler<ApplyDataRetentionCommand, DataRetentionResultDto>
{
    private readonly IDataRetentionService _retentionService;

    public ApplyDataRetentionCommandHandler(IDataRetentionService retentionService) =>
        _retentionService = retentionService;

    public Task<DataRetentionResultDto> Handle(
        ApplyDataRetentionCommand request,
        CancellationToken cancellationToken) =>
        _retentionService.ApplyAsync(request.DryRun, cancellationToken);
}
