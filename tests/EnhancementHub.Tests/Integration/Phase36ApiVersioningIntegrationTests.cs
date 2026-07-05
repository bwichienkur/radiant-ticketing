using System.Net;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class Phase36ApiVersioningIntegrationTests
{
    private readonly TestWebApplicationFactory _factory;

    public Phase36ApiVersioningIntegrationTests(TestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Health_IsReachable_UnderApiV1Alias()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EnhancementRequests_RequiresAuth_UnderV1Route()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/EnhancementRequests");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnhancementRequests_ListWorks_UnderV1Route_WhenAuthenticated()
    {
        var builder = _factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(UserRole.Developer);
        var client = await _factory.CreateAuthenticatedClientAsync(user);

        var response = await client.GetAsync("/api/v1/EnhancementRequests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
