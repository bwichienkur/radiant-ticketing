using System.Text;
using EnhancementHub.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnhancementHub.Tests.Unit;

public sealed class PolicyIntakeTests
{
    [Fact]
    public async Task DocumentTextExtractor_AcceptsPlainText()
    {
        var extractor = new DocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Retention policy: 7 years for audit logs."));

        var result = await extractor.ExtractAsync("policy.txt", stream);

        result.Succeeded.Should().BeTrue();
        result.Text.Should().Contain("Retention policy");
    }

    [Fact]
    public async Task DocumentTextExtractor_RejectsUnsupportedExtension()
    {
        var extractor = new DocumentTextExtractor();
        await using var stream = new MemoryStream([0x00, 0x01]);

        var result = await extractor.ExtractAsync("policy.docx", stream);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("PDF, TXT, or Markdown");
    }

    [Fact]
    public async Task PolicyUrlFetcher_BlocksLocalhost()
    {
        var fetcher = CreateUrlFetcher([]);
        var result = await fetcher.FetchAsync("http://localhost/policy");

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not allowed");
    }

    [Fact]
    public void SpaIntakeBff_ExposesPolicyEndpoints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaIntakeController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("policy-document");
        content.Should().Contain("policy-url");
        content.Should().Contain("AttachPolicyDocumentCommand");
        content.Should().Contain("AttachPolicyUrlCommand");
    }

    [Fact]
    public void IntakeCopilotPanel_IncludesPolicyIntakeUi()
    {
        var panel = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src/components/IntakeCopilotPanel.tsx"));
        panel.Should().Contain("Compliance policy intake");
        panel.Should().Contain("attachIntakePolicyDocument");
        panel.Should().Contain("attachIntakePolicyUrl");
    }

    private static PolicyUrlFetcher CreateUrlFetcher(string[] allowedHosts)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PolicyIntake:AllowedHosts:0"] = allowedHosts.Length > 0 ? allowedHosts[0] : null
            })
            .Build();

        return new PolicyUrlFetcher(
            new StubHttpClientFactory(),
            configuration,
            NullLogger<PolicyUrlFetcher>.Instance);
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
