using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class EnhancementRequestApiTests
{
    private readonly TestWebApplicationFactory _factory;

    public EnhancementRequestApiTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_ReturnsCreatedRequest_WhenAuthenticated()
    {
        var builder = _factory.CreateDataBuilder();
        var submitter = await builder.CreateUserAsync(UserRole.Submitter);
        var client = await _factory.CreateAuthenticatedClientAsync(submitter);

        var response = await client.PostAsJsonAsync("/api/enhancementrequests", new
        {
            title = "Add CSV export",
            businessDescription = "Users need to export order history.",
            desiredOutcome = "CSV download from orders page.",
            priority = "High"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<EnhancementRequestDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("Add CSV export");
        created.Status.Should().Be(EnhancementRequestStatus.Submitted);
        created.SubmittedByUserId.Should().Be(submitter.Id);
    }

    [Fact]
    public async Task List_ReturnsSeededRequests_WhenAuthenticated()
    {
        var builder = _factory.CreateDataBuilder();
        var submitter = await builder.CreateUserAsync(UserRole.Submitter);
        await builder.CreateEnhancementRequestAsync(submitter, title: "First request");
        await builder.CreateEnhancementRequestAsync(submitter, title: "Second request");

        var client = await _factory.CreateAuthenticatedClientAsync(submitter);
        var response = await client.GetAsync("/api/enhancementrequests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<EnhancementRequestDto>>();
        requests.Should().NotBeNull();
        requests!.Select(r => r.Title).Should().Contain(["First request", "Second request"]);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/enhancementrequests", new
        {
            title = "Unauthorized",
            businessDescription = "Should fail.",
            desiredOutcome = "No auth.",
            priority = "Low"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
