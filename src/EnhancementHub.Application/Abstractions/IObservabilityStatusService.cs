using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IObservabilityStatusService
{
    Task<ObservabilityStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
