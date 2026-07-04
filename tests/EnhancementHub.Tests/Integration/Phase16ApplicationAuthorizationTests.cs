using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class Phase16ApplicationAuthorizationTests
{
    private readonly TestWebApplicationFactory _factory;

    public Phase16ApplicationAuthorizationTests(TestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task ListApplications_ReturnsEmpty_ForUserWithoutTeamMembership()
    {
        var builder = _factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(UserRole.Developer);
        var client = await _factory.CreateAuthenticatedClientAsync(user);

        var response = await client.GetAsync("/api/applications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apps = await response.Content.ReadFromJsonAsync<List<object>>();
        apps.Should().NotBeNull();
        apps!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSystemMap_ReturnsNotFound_ForInaccessibleApplication()
    {
        var builder = _factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(UserRole.Developer);
        var client = await _factory.CreateAuthenticatedClientAsync(user);

        var response = await client.GetAsync($"/api/system-map/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
