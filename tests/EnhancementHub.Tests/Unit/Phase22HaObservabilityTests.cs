using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.DependencyInjection;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase22HaObservabilityTests
{
    [Fact]
    public void DataProtection_ResolvesAzureBlobProvider()
    {
        var options = new DataProtectionStorageOptions
        {
            StorageProvider = "AzureBlob",
            AzureBlob = new AzureBlobKeyStorageOptions { ConnectionString = "UseDevelopmentStorage=true" }
        };

        DataProtectionServiceExtensions.ResolveStorageProvider(options)
            .Should().Be(DataProtectionStorageProvider.AzureBlob);
    }

    [Fact]
    public void DataProtection_DefaultsToFileSystem()
    {
        var options = new DataProtectionStorageOptions { StorageProvider = "FileSystem" };

        DataProtectionServiceExtensions.ResolveStorageProvider(options)
            .Should().Be(DataProtectionStorageProvider.FileSystem);
    }

    [Fact]
    public void ProductionConfigurationValidator_AcceptsAzureBlobInsteadOfKeysPath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "production-secret-that-is-long-enough-32chars",
                ["DataProtection:StorageProvider"] = "AzureBlob",
                ["DataProtection:AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true"
            })
            .Build();

        var act = () => ProductionConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ObservabilityStatus_ReportsHaRecommendationsWhenDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Enabled"] = "false",
                ["Database:Provider"] = "Sqlite"
            })
            .Build();

        var service = new ObservabilityStatusService(
            configuration,
            Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()),
            Microsoft.Extensions.Options.Options.Create(new DataProtectionStorageOptions()));

        var status = await service.GetStatusAsync();

        status.OpenTelemetry.Enabled.Should().BeFalse();
        status.HighAvailability.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ObservabilityEndpoint_ReturnsStatusForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/observability/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("openTelemetry");
        json.Should().Contain("highAvailability");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnhancementHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
