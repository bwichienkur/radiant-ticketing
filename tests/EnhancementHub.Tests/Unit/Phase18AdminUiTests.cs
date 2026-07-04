using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase18AdminUiTests
{
    [Fact]
    public void AuthenticationConfigurationService_ReportsDisabledByDefault()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OpenIdConnect:Enabled"] = "false"
            })
            .Build();

        var service = new AuthenticationConfigurationService(configuration);
        var status = service.GetStatus();

        status.OpenIdConnectEnabled.Should().BeFalse();
        status.Issues.Should().Contain(i => i.Severity == "Info");
    }

    [Fact]
    public void AuthenticationConfigurationService_FlagsInvalidRoleMapping()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OpenIdConnect:Enabled"] = "true",
                ["Authentication:OpenIdConnect:Authority"] = "https://login.microsoftonline.com/tenant/v2.0",
                ["Authentication:OpenIdConnect:ClientId"] = "client",
                ["Authentication:OpenIdConnect:ClientSecret"] = "secret",
                ["Authentication:OpenIdConnect:DefaultRole"] = "Developer",
                ["Authentication:OpenIdConnect:RoleMappings:00000000-0000-0000-0000-000000000099"] = "NotARealRole"
            })
            .Build();

        var status = new AuthenticationConfigurationService(configuration).GetStatus();

        status.IsProductionReady.Should().BeFalse();
        status.RoleMappings.Should().ContainSingle(m => m.IsValidTargetRole == false);
        status.Issues.Should().Contain(i => i.Severity == "Error" && i.Message.Contains("NotARealRole"));
    }

    [Fact]
    public void AuthenticationConfigurationService_AcceptsValidEntraConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OpenIdConnect:Enabled"] = "true",
                ["Authentication:OpenIdConnect:Authority"] = "https://login.microsoftonline.com/tenant/v2.0",
                ["Authentication:OpenIdConnect:ClientId"] = "client",
                ["Authentication:OpenIdConnect:ClientSecret"] = "secret",
                ["Authentication:OpenIdConnect:DefaultRole"] = "Developer",
                ["Authentication:OpenIdConnect:RoleMappings:00000000-0000-0000-0000-000000000001"] = "Admin"
            })
            .Build();

        var status = new AuthenticationConfigurationService(configuration).GetStatus();

        status.IsProductionReady.Should().BeTrue();
        status.RoleMappings.Should().ContainSingle(m => m.IsValidTargetRole && m.IsGuidFormat);
    }

    [Fact]
    public async Task AdminAuthenticationStatusEndpoint_ReturnsStatusForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/authentication/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("openIdConnectEnabled");
    }

    [Fact]
    public async Task RetryBackgroundJob_ReturnsNotFoundForPollingProvider()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.PostAsync("/api/admin/jobs/nonexistent/retry", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
