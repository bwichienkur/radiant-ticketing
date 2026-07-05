using EnhancementHub.Infrastructure.Services.Integrations;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using System.Net.Http.Json;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase23IntegrationTests
{
    private const string SampleOpenApi = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Orders API", "version": "1.0" },
          "servers": [{ "url": "https://api.example.com" }],
          "paths": {
            "/orders": {
              "get": {
                "operationId": "listOrders",
                "summary": "List orders",
                "tags": ["Orders"]
              },
              "post": {
                "operationId": "createOrder",
                "summary": "Create order"
              }
            },
            "/orders/{id}": {
              "get": {
                "summary": "Get order"
              }
            }
          }
        }
        """;

    [Fact]
    public void OpenApiIngestion_ParsesPathsAndMethods()
    {
        var endpoints = OpenApiIngestionService.ParseEndpoints(SampleOpenApi);

        endpoints.Should().HaveCount(3);
        endpoints.Should().Contain(e => e.Path == "/orders" && e.HttpMethod == "GET");
        endpoints.Should().Contain(e => e.OperationId == "listOrders");
    }

    [Fact]
    public void OpenApiIngestion_ExtractsBaseUrl()
    {
        OpenApiIngestionService.ExtractBaseUrl(SampleOpenApi)
            .Should().Be("https://api.example.com");
    }

    [Fact]
    public void GitHubWebhook_VerifiesHmacSignature()
    {
        const string secret = "test-secret";
        const string payload = """{"repository":{"full_name":"org/repo"}}""";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        GitHubWebhookService.VerifySignature(payload, signature, secret).Should().BeTrue();
        GitHubWebhookService.VerifySignature(payload, "sha256=invalid", secret).Should().BeFalse();
    }

    [Fact]
    public void ChatIntake_ParsesPipeDelimitedText()
    {
        var (title, description) = ChatIntakeService.ParseIntakeText("New feature | Add export to CSV for reports");
        title.Should().Be("New feature");
        description.Should().Be("Add export to CSV for reports");
    }

    [Fact]
    public void ServiceNowSync_MapsApprovedState()
    {
        ServiceNowSyncService.MapState("approved")
            .Should().Be(Domain.Enums.EnhancementRequestStatus.Approved);
    }

    [Fact]
    public async Task OpenApiRegistrationEndpoint_RequiresAuth()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/integrations/openapi", new
        {
            applicationId = Guid.NewGuid(),
            name = "Test API",
            specDocument = SampleOpenApi
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TeamsIntake_CreatesRequestWhenEnabled()
    {
        await using var factory = new TeamsIntakeTestFactory();

        await factory.EnsureDatabaseInitializedAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-EnhancementHub-Intake-Key", "test-key");

        var response = await client.PostAsJsonAsync("/api/integrations/teams/intake", new
        {
            text = "Mobile login fix | Users cannot authenticate on iOS",
            userName = "teams-tester"
        });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("enhancementRequestId");
    }

    private sealed class TeamsIntakeTestFactory : TestWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?>? AdditionalSettings { get; } =
            new Dictionary<string, string?>
            {
                ["Integrations:Teams:Enabled"] = "true",
                ["Integrations:Teams:IntakeSecret"] = "test-key"
            };
    }
}
