using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> FindActiveByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid?> GetTenantIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
