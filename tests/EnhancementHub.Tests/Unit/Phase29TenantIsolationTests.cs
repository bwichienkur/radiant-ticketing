using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase29TenantIsolationTests
{
    [Fact]
    public void TenantSchemaNameResolver_BuildsValidSchemaFromSlug()
    {
        var options = new TenantIsolationOptions { SchemaPrefix = "tenant_" };
        var schema = TenantSchemaNameResolver.BuildSchemaName("acme-corp", options);
        schema.Should().Be("tenant_acme_corp");
        TenantSchemaNameResolver.IsValidSchemaName(schema).Should().BeTrue();
    }

    [Fact]
    public void TenantSchemaProvisioner_BuildSearchPathSql_QuotesSchema()
    {
        TenantSchemaProvisioner.BuildSearchPathSql("tenant_acme")
            .Should().Be("""SET search_path TO "tenant_acme", public;""");
    }

    [Fact]
    public async Task TenantIsolation_Provision_UpdatesTenantOnSqlite()
    {
        await using var factory = new TenantIsolationTestFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Isolation Org",
                Slug = "isolation-org",
                Plan = TenantPlan.Enterprise,
                Region = TenantRegion.EU,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var isolation = scope.ServiceProvider.GetRequiredService<ITenantIsolationService>();
            var status = await isolation.ProvisionDedicatedSchemaAsync(tenantId);
            status.IsolationMode.Should().Be(nameof(TenantIsolationMode.DedicatedSchema));
            status.IsSchemaProvisioned.Should().BeTrue();
            status.DatabaseSchemaName.Should().Be("tenant_isolation_org");
        }
    }

    [Fact]
    public async Task TenantIsolation_AutoProvision_SkipsWhenDisabled()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "No Auto Org",
                Slug = "no-auto",
                Plan = TenantPlan.Enterprise,
                Region = TenantRegion.US,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var isolation = scope.ServiceProvider.GetRequiredService<ITenantIsolationService>();
            await isolation.TryAutoProvisionAsync(tenantId);

            var tenant = await db.Tenants.FindAsync(tenantId);
            tenant!.SchemaProvisionedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task TenantIsolation_Endpoint_RequiresAuthentication()
    {
        await using var factory = new TenantIsolationTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tenants/current/isolation");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantIsolation_ReturnsStatusForTenantAdmin()
    {
        await using var factory = new TenantIsolationTestFactory();
        await factory.EnsureDatabaseInitializedAsync();
        var builder = factory.CreateDataBuilder();
        var tenantId = Guid.NewGuid();

        User admin;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Status Org",
                Slug = "status-org",
                Plan = TenantPlan.Team,
                Region = TenantRegion.US,
                IsolationMode = TenantIsolationMode.SharedRowLevel,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        admin = await builder.CreateUserAsync(UserRole.Admin, email: $"isolation-{Guid.NewGuid():N}@test.local");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            var user = await db.Users.FindAsync(admin.Id);
            user!.TenantId = tenantId;
            await db.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/tenants/current/isolation");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        json.GetProperty("isolationMode").GetString().Should().Be("SharedRowLevel");
        json.GetProperty("isolationEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void TenancyAdminPage_ShowsIsolationControls()
    {
        var page = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Admin/Tenancy.cshtml"));

        page.Should().Contain("Data isolation");
        page.Should().Contain("Provision dedicated schema");
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

    private sealed class TenantIsolationTestFactory : TestWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?>? AdditionalSettings { get; } =
            new Dictionary<string, string?>
            {
                ["TenantIsolation:Enabled"] = "true",
                ["TenantIsolation:AutoProvisionEnterprise"] = "false"
            };
    }
}
