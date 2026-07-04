using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Common;

public sealed class TestDataBuilder
{
    private readonly TestWebApplicationFactory _factory;

    public TestDataBuilder(TestWebApplicationFactory factory) => _factory = factory;

    public async Task<User> CreateUserAsync(
        UserRole role,
        string? email = null,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        await _factory.EnsureDatabaseInitializedAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@test.local",
            DisplayName = displayName ?? $"{role} User",
            Role = role,
            IsActive = true,
            PasswordHash = hasher.Hash("password123"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<EnhancementRequest> CreateEnhancementRequestAsync(
        User submittedBy,
        EnhancementRequestStatus status = EnhancementRequestStatus.Submitted,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        await _factory.EnsureDatabaseInitializedAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();

        var request = new EnhancementRequest
        {
            Id = Guid.NewGuid(),
            Title = title ?? "Test enhancement request",
            BusinessDescription = "Business need for automated testing.",
            DesiredOutcome = "Reliable automated test coverage.",
            Priority = "Medium",
            SubmittedByUserId = submittedBy.Id,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = submittedBy.Id
        };

        db.EnhancementRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return request;
    }
}
