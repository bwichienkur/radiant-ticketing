using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IDataRetentionService
{
    Task<DataRetentionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<DataRetentionResultDto> ApplyAsync(bool dryRun = false, CancellationToken cancellationToken = default);
}
