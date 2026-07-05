using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class ProductHardeningTests
{
    [Fact]
    public async Task EnhancementRequestAccessService_FiltersRequestsByTenant_ForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Tenants.AddRange(
            new Tenant
            {
                Id = tenantA,
                Name = "Tenant A",
                Slug = "tenant-a",
                Plan = TenantPlan.Team,
                Region = TenantRegion.US,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Tenant
            {
                Id = tenantB,
                Name = "Tenant B",
                Slug = "tenant-b",
                Plan = TenantPlan.Team,
                Region = TenantRegion.US,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

        var adminA = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin-a@test.local",
            DisplayName = "Admin A",
            Role = UserRole.Admin,
            TenantId = tenantA,
            PasswordHash = "hash",
            CreatedAt = now,
            UpdatedAt = now
        };
        var submitterB = new User
        {
            Id = Guid.NewGuid(),
            Email = "submitter-b@test.local",
            DisplayName = "Submitter B",
            Role = UserRole.Submitter,
            TenantId = tenantB,
            PasswordHash = "hash",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.AddRange(adminA, submitterB);
        db.EnhancementRequests.AddRange(
            new EnhancementRequest
            {
                Id = Guid.NewGuid(),
                Title = "Tenant A request",
                BusinessDescription = "A",
                DesiredOutcome = "B",
                Priority = "Low",
                SubmittedByUserId = adminA.Id,
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = now,
                UpdatedAt = now
            },
            new EnhancementRequest
            {
                Id = Guid.NewGuid(),
                Title = "Tenant B request",
                BusinessDescription = "A",
                DesiredOutcome = "B",
                Priority = "Low",
                SubmittedByUserId = submitterB.Id,
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = now,
                UpdatedAt = now
            });
        await db.SaveChangesAsync();

        var accessService = new EnhancementRequestAccessService(
            db,
            new TestCurrentUser(adminA.Id, adminA.Email, adminA.DisplayName, UserRole.Admin),
            new TestCurrentTenant(tenantA));

        var visible = await accessService.ApplyVisibilityFilter(db.EnhancementRequests).ToListAsync();
        visible.Should().ContainSingle(r => r.SubmittedByUserId == adminA.Id);
        visible.Should().NotContain(r => r.SubmittedByUserId == submitterB.Id);
    }

    [Fact]
    public void Layout_IncludesMobileSidebarOffcanvas()
    {
        var layout = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));

        layout.Should().Contain("appSidebarOffcanvas");
        layout.Should().Contain("app-sidebar-offcanvas");
    }

    [Fact]
    public void DashboardChecklist_UsesActionableLinks()
    {
        var page = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Index.cshtml"));

        page.Should().Contain("checklist-item-link");
        page.Should().Contain("/Spa/OnboardingWizard");
        page.Should().Contain("/Spa/SystemMap");
    }

    [Fact]
    public void SpaDataController_ExposesApprovalHistoryBff()
    {
        var controller = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Controllers/SpaDataController.cs"));

        controller.Should().Contain("requests/{id:guid}/approval-history");
        controller.Should().Contain("GetApprovalHistoryQuery");
    }

    [Fact]
    public void RequestDetailApp_IncludesApprovalHistoryAndAnalysisSections()
    {
        var app = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src/apps/RequestDetailApp.tsx"));

        app.Should().Contain("getApprovalHistory");
        app.Should().Contain("Approval history");
        app.Should().Contain("AnalysisDetailSections");
    }

    [Fact]
    public void CommandPalette_SupportsKeyboardNavigation()
    {
        var siteJs = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/wwwroot/js/site.js"));

        siteJs.Should().Contain("ArrowDown");
        siteJs.Should().Contain("ArrowUp");
        siteJs.Should().Contain("navigateActiveResult");
    }

    [Fact]
    public void ProductionEvalProfile_Exists()
    {
        File.Exists(Path.Combine(GetRepoRoot(), "docker-compose.eval.yml")).Should().BeTrue();
        var doc = File.ReadAllText(Path.Combine(GetRepoRoot(), "docs/PRODUCTION_EVAL.md"));
        doc.Should().Contain("docker-compose.eval.yml");
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
        public TestCurrentUser(Guid userId, string email, string displayName, UserRole role)
        {
            UserId = userId;
            Email = email;
            DisplayName = displayName;
            Role = role;
            IsAuthenticated = true;
        }

        public Guid? UserId { get; }
        public string? Email { get; }
        public string? DisplayName { get; }
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
