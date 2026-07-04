using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IEnhancementHubDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string details,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId ?? Guid.Empty,
            Comments = details,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
