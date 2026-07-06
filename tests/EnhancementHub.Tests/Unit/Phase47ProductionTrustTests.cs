using EnhancementHub.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase47ProductionTrustTests
{
    [Fact]
    public void ProductionConfigurationValidator_RejectsInMemoryVectorInProduction()
    {
        var configuration = ValidProductionConfiguration();
        configuration["VectorSearch:Provider"] = "InMemory";

        var act = () => ValidateProduction(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*VectorSearch*");
    }

    [Fact]
    public void ProductionConfigurationValidator_RejectsSimulatedQaInProduction()
    {
        var configuration = ValidProductionConfiguration();
        configuration["Delivery:Qa:Runner"] = "Simulated";

        var act = () => ValidateProduction(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Delivery:Qa:Runner*");
    }

    [Fact]
    public void ProductionConfigurationValidator_RejectsMissingAiProviderInProduction()
    {
        var configuration = ValidProductionConfiguration();
        configuration.Remove("OpenAI:ApiKey");

        var act = () => ValidateProduction(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AI provider*");
    }

    [Fact]
    public void ProductionConfigurationValidator_AllowsMockAiWhenExplicitlyEnabled()
    {
        var configuration = ValidProductionConfiguration();
        configuration["AI:AllowMockInProduction"] = "true";

        var act = () => ValidateProduction(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void ProductionConfigurationValidator_AllowsInMemoryVectorWhenExplicitlyEnabled()
    {
        var configuration = ValidProductionConfiguration();
        configuration["VectorSearch:Provider"] = "InMemory";
        configuration["VectorSearch:AllowInMemoryInProduction"] = "true";
        configuration["AI:AllowMockInProduction"] = "true";

        var act = () => ValidateProduction(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void AiConfigurationReader_DetectsOpenAiKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "sk-test-key",
            })
            .Build();

        AiConfigurationReader.IsAiConfigured(configuration).Should().BeTrue();
        AiConfigurationReader.ResolveAiProviderLabel(configuration).Should().Be("OpenAI");
    }

    [Fact]
    public void PlatformRuntimeStatusService_ReportsSimulatedBackendsWhenAiMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VectorSearch:Provider"] = "InMemory",
                ["Delivery:Qa:Runner"] = "Simulated",
            })
            .Build();

        var service = new Infrastructure.Services.PlatformRuntimeStatusService(
            configuration,
            new Infrastructure.Services.ConfigurationFeatureService(configuration));
        var status = service.GetStatus();

        status.AiConfigured.Should().BeFalse();
        status.UsesSimulatedBackends.Should().BeTrue();
        status.VectorSearchProvider.Should().Be("InMemory");
        status.QaRunner.Should().Be("Simulated");
    }

    private static Dictionary<string, string?> ValidProductionConfiguration() => new()
    {
        ["Jwt:Secret"] = "production-secret-with-sufficient-length-32",
        ["DataProtection:KeysPath"] = "/tmp/keys",
        ["VectorSearch:Provider"] = "PgVector",
        ["Delivery:Qa:Runner"] = "Playwright",
        ["OpenAI:ApiKey"] = "sk-production-test",
    };

    private static void ValidateProduction(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ProductionConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment("Production"));
    }

    // Expose nested type for reuse
    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnhancementHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
