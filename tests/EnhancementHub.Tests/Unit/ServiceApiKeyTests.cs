using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class ServiceApiKeyTests
{
    [Fact]
    public async Task CreateServiceApiKey_ReturnsPlainKeyOnce()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.PostAsJsonAsync("/api/admin/api-keys", new
        {
            name = "CI Bot",
            description = "Pipeline integration",
            role = UserRole.Developer
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateServiceApiKeyResultDto>();
        result.Should().NotBeNull();
        result!.ApiKey.Should().StartWith(ApiKeyAuthenticationDefaults.KeyPrefix);
        result.KeyPrefix.Should().HaveLength(11);
    }

    [Fact]
    public async Task ApiKey_AuthenticatesAndAccessesScopedApplications()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "ApiKey Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Applications.Add(new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            Name = "ApiKey App",
            OwnerTeamId = teamId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var adminClient = await factory.CreateAuthenticatedClientAsync(admin);
        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/api-keys", new
        {
            name = "Scoped Bot",
            role = UserRole.Developer,
            teamId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateServiceApiKeyResultDto>();

        using var apiClient = factory.CreateClient();
        apiClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, created!.ApiKey);

        var appsResponse = await apiClient.GetAsync("/api/applications");
        appsResponse.EnsureSuccessStatusCode();
        var apps = await appsResponse.Content.ReadFromJsonAsync<List<object>>();
        apps.Should().NotBeNull();
        apps!.Should().HaveCount(1);
    }

    [Fact]
    public async Task RevokedApiKey_ReturnsUnauthorized()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        using var adminClient = await factory.CreateAuthenticatedClientAsync(admin);
        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/api-keys", new
        {
            name = "Temp Bot",
            role = UserRole.Developer
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateServiceApiKeyResultDto>();

        var revokeResponse = await adminClient.DeleteAsync($"/api/admin/api-keys/{created!.Id}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var apiClient = factory.CreateClient();
        apiClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, created.ApiKey);
        var appsResponse = await apiClient.GetAsync("/api/applications");
        appsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListServiceApiKeys_ForbiddenForNonAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var developer = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Developer);

        using var client = await factory.CreateAuthenticatedClientAsync(developer);
        var response = await client.GetAsync("/api/admin/api-keys");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InvalidApiKey_ReturnsUnauthorized()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "eh_invalidkeyvalue");

        var response = await client.GetAsync("/api/applications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
