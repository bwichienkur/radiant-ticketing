using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase26CommercialPlatformTests
{
    [Fact]
    public void TenantMeteringService_GetCurrentPeriodStart_UsesUtcMonthBoundary()
    {
        var utc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        TenantMeteringService.GetCurrentPeriodStart(utc)
            .Should().Be(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ApplicationAccessService_FiltersApplicationsByTenant()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Tenants.AddRange(
            new Tenant { Id = tenantA, Name = "Tenant A", Slug = "tenant-a", Plan = TenantPlan.Trial, Region = TenantRegion.US, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Tenant { Id = tenantB, Name = "Tenant B", Slug = "tenant-b", Plan = TenantPlan.Trial, Region = TenantRegion.EU, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.Users.Add(new User
        {
            Id = userId,
            Email = "tenant-a-user@test.local",
            DisplayName = "Tenant A User",
            Role = UserRole.Admin,
            TenantId = tenantA,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Teams.AddRange(
            new Team { Id = teamA, Name = "Team A", TenantId = tenantA, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Team { Id = teamB, Name = "Team B", TenantId = tenantB, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.Applications.AddRange(
            new Domain.Entities.Application
            {
                Id = Guid.NewGuid(),
                Name = "Tenant A App",
                OwnerTeamId = teamA,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.Application
            {
                Id = Guid.NewGuid(),
                Name = "Tenant B App",
                OwnerTeamId = teamB,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var accessService = new ApplicationAccessService(
            db,
            new TestCurrentUser(userId, UserRole.Admin),
            new TestCurrentTenant(tenantA));

        var visible = await accessService.ApplyVisibilityFilter(db.Applications).ToListAsync();
        visible.Should().ContainSingle(a => a.Name == "Tenant A App");
    }

    [Fact]
    public void JwtTokenGenerator_IncludesTenantClaimWhenPresent()
    {
        var tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var generator = new JwtTokenGenerator(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestWebApplicationFactory.JwtSecret,
                ["Jwt:Issuer"] = "EnhancementHub",
                ["Jwt:Audience"] = "EnhancementHub"
            })
            .Build());

        var token = generator.GenerateToken(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@tenant.test",
            DisplayName = "Admin",
            Role = UserRole.Admin,
            TenantId = tenantId
        });

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
    }

    [Fact]
    public async Task TenantRegister_Endpoint_CreatesTrialTenant()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        using var client = factory.CreateClient();

        var slug = $"trial-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/tenants/register", new
        {
            organizationName = "Acme Corp",
            slug,
            adminEmail = $"{slug}@test.local",
            adminPassword = "password123",
            adminDisplayName = "Acme Admin",
            region = TenantRegion.US
        });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Trial");
        json.Should().Contain(slug);
    }

    [Fact]
    public async Task TenantBilling_RequiresAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tenants/current/billing");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantBilling_ReturnsUsageForAuthenticatedTenantAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
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
                Name = "Billing Test Org",
                Slug = $"billing-{Guid.NewGuid():N}"[..24],
                Plan = TenantPlan.Trial,
                Region = TenantRegion.US,
                TrialEndsAt = DateTime.UtcNow.AddDays(14),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        admin = await builder.CreateUserAsync(UserRole.Admin, email: $"billing-admin-{Guid.NewGuid():N}@test.local");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            var user = await db.Users.FindAsync(admin.Id);
            user!.TenantId = tenantId;
            await db.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/tenants/current/billing");
        response.EnsureSuccessStatusCode();

        var billing = await response.Content.ReadFromJsonAsync<JsonElement>();
        billing.GetProperty("tenantId").GetGuid().Should().Be(tenantId);
        billing.GetProperty("plan").GetString().Should().Be("Trial");
    }

    [Fact]
    public void CommercialOptions_HasPlanLimits()
    {
        var options = new CommercialOptions();
        options.TrialLimits.MaxApplications.Should().BeGreaterThan(0);
        options.EnterpriseLimits.MaxAnalysesPerMonth.Should().BeGreaterThan(options.TrialLimits.MaxAnalysesPerMonth);
    }

    [Fact]
    public void SignupPage_AllowsSelfServiceRegistration()
    {
        var signup = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Account/Signup.cshtml"));

        signup.Should().Contain("OrganizationName");
        signup.Should().Contain("Start your trial");
    }

    [Fact]
    public void TenancyAdminPage_ListsBillingAndTenants()
    {
        var page = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Admin/Tenancy.cshtml"));

        page.Should().Contain("PageHeaderTitle");
        page.Should().Contain("Tenancy & billing");
        page.Should().Contain("Platform administrator view");
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

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(Guid userId, UserRole role)
        {
            UserId = userId;
            Role = role;
            IsAuthenticated = true;
        }

        public Guid? UserId { get; }
        public string? Email => "test@test.local";
        public string? DisplayName => "Test User";
        public UserRole? Role { get; }
        public bool IsAuthenticated { get; }
        public string? IpAddress => "127.0.0.1";
    }

    private sealed class TestCurrentTenant : ICurrentTenantService
    {
        public TestCurrentTenant(Guid? tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public bool HasTenantContext => TenantId.HasValue;
    }
}
