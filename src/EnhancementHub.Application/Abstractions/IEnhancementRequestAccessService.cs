using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions;

public interface IEnhancementRequestAccessService
{
    IQueryable<EnhancementRequest> ApplyVisibilityFilter(IQueryable<EnhancementRequest> query);

    Task<EnhancementRequest> GetAccessibleRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task EnsureCanModifyAsync(Guid requestId, CancellationToken cancellationToken = default);
}
