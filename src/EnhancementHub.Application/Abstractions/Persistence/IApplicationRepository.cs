using EnhancementHub.Domain.Entities;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface IApplicationRepository
{
    Task<IReadOnlyList<ApplicationEntity>> ListWithRepositoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationProfile>> ListProfilesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
