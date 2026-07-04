using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface ISystemGraphBuilder
{
    Task<SystemGraphDto> BuildForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<SystemGraphDto> BuildForRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default);
}
