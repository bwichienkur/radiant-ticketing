using EnhancementHub.Application.Features.Reporting.Queries;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase16SsoJobsDashboardTests
{
    [Fact]
    public void ProductionConfigurationValidator_RequiresOidcSettingsWhenEnabled()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "production-secret-with-enough-characters-for-validation",
            ["DataProtection:KeysPath"] = "/tmp/keys",
            ["Authentication:OpenIdConnect:Enabled"] = "true"
        };

        var act = () => ProductionConfigurationValidator.Validate(
            new ConfigurationBuilder().AddInMemoryCollection(configuration).Build(),
            new TestHostEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OpenIdConnect:Authority*");
    }

    [Fact]
    public void ProductionConfigurationValidator_AllowsOidcWhenFullyConfigured()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "production-secret-with-enough-characters-for-validation",
            ["DataProtection:KeysPath"] = "/tmp/keys",
            ["Authentication:OpenIdConnect:Enabled"] = "true",
            ["Authentication:OpenIdConnect:Authority"] = "https://login.microsoftonline.com/tenant/v2.0",
            ["Authentication:OpenIdConnect:ClientId"] = "client-id",
            ["Authentication:OpenIdConnect:ClientSecret"] = "client-secret",
            ["Authentication:OpenIdConnect:DefaultRole"] = "Developer"
        };
        ProductionConfigurationTestDefaults.ApplyProductionBackendDefaults(configuration);

        var act = () => ProductionConfigurationValidator.Validate(
            new ConfigurationBuilder().AddInMemoryCollection(configuration).Build(),
            new TestHostEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task BackgroundJobStatusService_ReturnsPollingProviderAndJobs()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var service = new BackgroundJobStatusService(db, configuration);

        var status = await service.GetStatusAsync();

        status.Provider.Should().Be("Polling");
        status.Jobs.Should().HaveCount(6);
        status.QueueCounts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardReport_IncludesAwaitingAnalysisAndHighRiskPendingApproval()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        await factory.EnsureDatabaseInitializedAsync();

        var submitter = await builder.CreateUserAsync(UserRole.Developer);
        var submitted = await builder.CreateEnhancementRequestAsync(
            submitter,
            EnhancementRequestStatus.Submitted,
            "Awaiting analysis");
        var pendingHighRisk = await builder.CreateEnhancementRequestAsync(
            submitter,
            EnhancementRequestStatus.PendingApproval,
            "High risk pending");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.EnhancementAnalyses.Add(new EnhancementAnalysis
            {
                Id = Guid.NewGuid(),
                EnhancementRequestId = pendingHighRisk.Id,
                Version = 1,
                RiskLevel = RiskLevel.High,
                ConfidenceScore = 0.9,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var mediatorScope = factory.Services.CreateScope();
        var mediator = mediatorScope.ServiceProvider.GetRequiredService<IMediator>();
        var report = await mediator.Send(new GetDashboardReportQuery());

        report.AwaitingAnalysisCount.Should().BeGreaterThanOrEqualTo(1);
        report.HighRiskPendingApprovalCount.Should().Be(1);
        report.TotalRequests.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task AdminJobsStatusEndpoint_RequiresAdminRole()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var developer = await builder.CreateUserAsync(UserRole.Developer);

        using var client = await factory.CreateAuthenticatedClientAsync(developer);
        var response = await client.GetAsync("/api/admin/jobs/status");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminJobsStatusEndpoint_ReturnsStatusForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/jobs/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("provider");
        json.Should().Contain("queueCounts");
    }

    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnhancementHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
