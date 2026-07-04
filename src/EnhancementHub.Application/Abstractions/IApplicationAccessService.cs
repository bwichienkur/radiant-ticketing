using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Abstractions;

public interface IApplicationAccessService
{
    IQueryable<ApplicationEntity> ApplyVisibilityFilter(IQueryable<ApplicationEntity> query);

    Task<ApplicationEntity> GetAccessibleApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task EnsureAccessibleApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task EnsureAccessibleConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task EnsureAccessibleRefactorPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default);
}
