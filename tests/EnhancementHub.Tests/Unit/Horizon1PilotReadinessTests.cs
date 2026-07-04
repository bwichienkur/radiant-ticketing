using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class Horizon1PilotReadinessTests
{
    [Fact]
    public async Task ApplicationAccessService_FiltersApplicationsByTeamOwnership()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = memberUserId,
            Email = "member@test.local",
            DisplayName = "Member",
            Role = UserRole.Developer,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Teams.AddRange(
            new Team { Id = teamId, Name = "Owned", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Team { Id = otherTeamId, Name = "Other", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.TeamMembers.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = memberUserId,
            Role = "Member",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Applications.AddRange(
            new Domain.Entities.Application
            {
                Id = Guid.NewGuid(),
                Name = "Visible App",
                OwnerTeamId = teamId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.Application
            {
                Id = Guid.NewGuid(),
                Name = "Hidden App",
                OwnerTeamId = otherTeamId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var accessService = new ApplicationAccessService(
            db,
            new TestCurrentUser(memberUserId, UserRole.Developer));

        var visible = await accessService.ApplyVisibilityFilter(db.Applications).ToListAsync();
        visible.Should().ContainSingle(a => a.Name == "Visible App");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(Guid userId, UserRole role)
        {
            UserId = userId;
            Role = role;
            IsAuthenticated = true;
        }

        public Guid? UserId { get; }
        public string? Email => "member@test.local";
        public string? DisplayName => "Member";
        public UserRole? Role { get; }
        public bool IsAuthenticated { get; }
        public string? IpAddress => "127.0.0.1";
    }
}
