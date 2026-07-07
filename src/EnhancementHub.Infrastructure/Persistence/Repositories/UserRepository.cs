using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly EnhancementHubDbContext _dbContext;

    public UserRepository(EnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public Task<User?> FindActiveByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email && u.IsActive,
            cancellationToken);

    public Task<Guid?> GetTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
}
