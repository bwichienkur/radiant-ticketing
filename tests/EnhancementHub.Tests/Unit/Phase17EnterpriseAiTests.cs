using EnhancementHub.Infrastructure.Services.Ai;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase17EnterpriseAiTests
{
    [Fact]
    public void PiiRedactionService_RedactsEmailAndPhone()
    {
        var service = new PiiRedactionService(Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Options.AiOptions { PiiRedactionEnabled = true }));

        var input = "Contact admin@company.com or call 555-123-4567 for access.";
        var redacted = service.Redact(input);

        redacted.Should().NotContain("admin@company.com");
        redacted.Should().Contain("[REDACTED_EMAIL]");
        redacted.Should().Contain("[REDACTED_PHONE]");
    }

    [Fact]
    public void AiCostEstimator_ReturnsNonZeroForKnownModel()
    {
        var cost = AiCostEstimator.Estimate("gpt-4o-mini", 1000, 500);
        cost.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void ChatCompletionService_IsNotConfigured_WhenNoApiKey()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "",
                ["AI:OpenAI:ApiKey"] = ""
            })
            .Build();

        var service = new ChatCompletionService(
            new TestHttpClientFactory(),
            configuration,
            Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.AiOptions()),
            new PiiRedactionService(Microsoft.Extensions.Options.Options.Create(
                new Infrastructure.Options.AiOptions())),
            new TestBudgetService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatCompletionService>.Instance);

        service.IsConfigured.Should().BeFalse();
    }

    private sealed class TestBudgetService : Application.Abstractions.IAiUsageBudgetService
    {
        public Task EnsureWithinBudgetAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TestHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
