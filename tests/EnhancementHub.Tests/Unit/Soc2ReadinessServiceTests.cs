using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EnhancementHub.Tests.Unit;

public sealed class Soc2ReadinessServiceTests
{
    [Fact]
    public void GetReport_ReturnsControlsWithImplementedAndPartialStatuses()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "production-secret-with-enough-characters-for-validation",
                ["DataProtection:KeysPath"] = "/tmp/keys",
                ["Retention:Enabled"] = "true",
                ["AI:Budget:Enabled"] = "true"
            })
            .Build();

        var runtimeStatus = new PlatformRuntimeStatusService(configuration);
        var service = new Soc2ReadinessService(configuration, new TestHostEnvironment("Production"), runtimeStatus);
        var report = service.GetReport();

        report.Controls.Should().NotBeEmpty();
        report.ImplementedCount.Should().BeGreaterThan(0);
        report.Controls.Should().Contain(c => c.ControlId == "CC7.2" && c.Title.Contains("Audit"));
    }

    [Fact]
    public async Task AdminSoc2Endpoint_ReturnsReportForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/compliance/soc2");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("implementedCount");
        json.Should().Contain("controls");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnhancementHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
