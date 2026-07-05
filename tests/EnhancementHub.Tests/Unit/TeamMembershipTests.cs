using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class TeamMembershipTests
{
    [Fact]
    public async Task ListTeams_ReturnsTeamsForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Test Team Alpha",
            Description = "For membership tests",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/teams");

        response.EnsureSuccessStatusCode();
        var teams = await response.Content.ReadFromJsonAsync<List<TeamSummaryDto>>();
        teams.Should().Contain(t => t.Name == "Test Team Alpha");
    }

    [Fact]
    public async Task AddTeamMember_CreatesUserAndGrantsApplicationAccess()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Access Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Applications.Add(new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            Name = "Scoped App",
            OwnerTeamId = teamId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var adminClient = await factory.CreateAuthenticatedClientAsync(admin);
        var inviteResponse = await adminClient.PostAsJsonAsync($"/api/admin/teams/{teamId}/members", new
        {
            email = "newmember@test.local",
            displayName = "New Member",
            globalRole = UserRole.Developer,
            teamRole = TeamMemberRoles.Member
        });

        inviteResponse.EnsureSuccessStatusCode();
        var inviteResult = await inviteResponse.Content.ReadFromJsonAsync<AddTeamMemberResultDto>();
        inviteResult.Should().NotBeNull();
        inviteResult!.UserCreated.Should().BeTrue();
        inviteResult.TemporaryPassword.Should().Be(AddTeamMemberCommandHandler.DefaultInvitePassword);

        var invitedUser = await db.Users.FirstAsync(u => u.Email == "newmember@test.local");
        using var memberClient = await factory.CreateAuthenticatedClientAsync(invitedUser);
        var appsResponse = await memberClient.GetAsync("/api/applications");

        appsResponse.EnsureSuccessStatusCode();
        var apps = await appsResponse.Content.ReadFromJsonAsync<List<object>>();
        apps.Should().NotBeNull();
        apps!.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddTeamMember_RejectsDuplicateMembership()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);
        var existing = await builder.CreateUserAsync(UserRole.Developer, "dup@test.local");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Dup Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.TeamMembers.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = existing.Id,
            Role = TeamMemberRoles.Member,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.PostAsJsonAsync($"/api/admin/teams/{teamId}/members", new
        {
            email = "dup@test.local",
            displayName = existing.DisplayName,
            globalRole = UserRole.Developer,
            teamRole = TeamMemberRoles.Member
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTeamMemberRole_PersistsNewRole()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);
        var member = await builder.CreateUserAsync(UserRole.Developer);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Role Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.TeamMembers.Add(new TeamMember
        {
            Id = membershipId,
            TeamId = teamId,
            UserId = member.Id,
            Role = TeamMemberRoles.Member,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.PutAsJsonAsync(
            $"/api/admin/teams/{teamId}/members/{membershipId}",
            new { teamRole = TeamMemberRoles.Lead });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = await db.TeamMembers.AsNoTracking().FirstAsync(m => m.Id == membershipId);
        updated.Role.Should().Be(TeamMemberRoles.Lead);
    }

    [Fact]
    public async Task RemoveTeamMember_RevokesApplicationAccess()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);
        var member = await builder.CreateUserAsync(UserRole.Developer);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Remove Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.TeamMembers.Add(new TeamMember
        {
            Id = membershipId,
            TeamId = teamId,
            UserId = member.Id,
            Role = TeamMemberRoles.Member,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Applications.Add(new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            Name = "Protected App",
            OwnerTeamId = teamId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var adminClient = await factory.CreateAuthenticatedClientAsync(admin);
        var removeResponse = await adminClient.DeleteAsync($"/api/admin/teams/{teamId}/members/{membershipId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var memberClient = await factory.CreateAuthenticatedClientAsync(member);
        var appsResponse = await memberClient.GetAsync("/api/applications");
        appsResponse.EnsureSuccessStatusCode();
        var apps = await appsResponse.Content.ReadFromJsonAsync<List<object>>();
        apps.Should().NotBeNull();
        apps!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListTeams_ForbiddenForNonAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var developer = await builder.CreateUserAsync(UserRole.Developer);

        using var client = await factory.CreateAuthenticatedClientAsync(developer);
        var response = await client.GetAsync("/api/admin/teams");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
