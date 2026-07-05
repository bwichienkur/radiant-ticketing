using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IDataScalingStatusService
{
    Task<DataScalingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
