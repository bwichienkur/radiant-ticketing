using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class Phase15AuthorizationIntegrationTests
{
    private readonly TestWebApplicationFactory _factory;

    public Phase15AuthorizationIntegrationTests(TestWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetEnhancementRequest_ReturnsNotFound_ForOtherUsersRequest()
    {
        var builder = _factory.CreateDataBuilder();
        var owner = await builder.CreateUserAsync(UserRole.Submitter);
        var other = await builder.CreateUserAsync(UserRole.Submitter);
        var request = await builder.CreateEnhancementRequestAsync(owner);

        var client = await _factory.CreateAuthenticatedClientAsync(other);
        var response = await client.GetAsync($"/api/enhancementrequests/{request.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OnPremAgentScanResults_ReturnsUnauthorized_WithoutApiKey()
    {
        await _factory.EnsureDatabaseInitializedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/on-prem-agent/{Guid.NewGuid()}/scan-results",
            new
            {
                connectionId = Guid.NewGuid(),
                scanResult = new DatabaseSchemaScanResult()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
