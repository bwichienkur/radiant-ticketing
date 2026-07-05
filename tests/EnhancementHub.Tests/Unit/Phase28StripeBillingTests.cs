using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

namespace EnhancementHub.Tests.Unit;

public sealed class Phase28StripeBillingTests
{
    [Fact]
    public void StripeOptions_HasPriceConfiguration()
    {
        var options = new StripeOptions();
        options.Prices.Team.Should().NotBeNull();
        options.Prices.Enterprise.Should().NotBeNull();
    }

    [Fact]
    public void StripeWebhook_VerifiesSignatureWhenSecretConfigured()
    {
        const string secret = "whsec_test_secret";
        const string payload = """{"type":"checkout.session.completed"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        var signature = $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";

        StripeBillingService.VerifyWebhookSignature(payload, signature, secret).Should().BeTrue();
        StripeBillingService.VerifyWebhookSignature(payload, "t=1,v1=invalid", secret).Should().BeFalse();
    }

    [Fact]
    public void StripeWebhook_SkipsVerificationWhenSecretMissing()
    {
        StripeBillingService.VerifyWebhookSignature("{}", signatureHeader: null, webhookSecret: null)
            .Should().BeTrue();
    }

    [Fact]
    public void StripeBilling_MapsSubscriptionStatuses()
    {
        StripeBillingService.MapSubscriptionStatus("active", "customer.subscription.updated")
            .Should().Be(TenantSubscriptionStatus.Active);
        StripeBillingService.MapSubscriptionStatus("active", "customer.subscription.deleted")
            .Should().Be(TenantSubscriptionStatus.Canceled);
    }

    [Fact]
    public void TenantBillingService_HasActiveSubscription_IncludesPaidPlans()
    {
        var teamTenant = new Tenant { Plan = TenantPlan.Team, SubscriptionStatus = TenantSubscriptionStatus.None };
        TenantBillingService.HasActiveSubscription(teamTenant).Should().BeTrue();

        var trialTenant = new Tenant { Plan = TenantPlan.Trial, SubscriptionStatus = TenantSubscriptionStatus.Active };
        TenantBillingService.HasActiveSubscription(trialTenant).Should().BeTrue();

        var expiredTrial = new Tenant { Plan = TenantPlan.Trial, SubscriptionStatus = TenantSubscriptionStatus.None };
        TenantBillingService.HasActiveSubscription(expiredTrial).Should().BeFalse();
    }

    [Fact]
    public async Task TenantBillingService_BlocksExpiredTrial()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Expired Trial Org",
            Slug = $"expired-{Guid.NewGuid():N}"[..20],
            Plan = TenantPlan.Trial,
            Region = TenantRegion.US,
            TrialEndsAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var billingService = scope.ServiceProvider.GetRequiredService<ITenantBillingService>();
        var act = async () => await billingService.EnsureWithinLimitsAsync(tenantId);
        await act.Should().ThrowAsync<Application.Common.Exceptions.ForbiddenException>()
            .WithMessage("*Trial has expired*");
    }

    [Fact]
    public async Task BillingCheckout_RequiresAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/billing/checkout", new { plan = TenantPlan.Team });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BillingCheckout_ReturnsForbiddenWhenStripeDisabled()
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
                Name = "Checkout Test Org",
                Slug = $"checkout-{Guid.NewGuid():N}"[..20],
                Plan = TenantPlan.Trial,
                Region = TenantRegion.US,
                TrialEndsAt = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        admin = await builder.CreateUserAsync(UserRole.Admin, email: $"checkout-{Guid.NewGuid():N}@test.local");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            var user = await db.Users.FindAsync(admin.Id);
            user!.TenantId = tenantId;
            await db.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.PostAsJsonAsync("/api/billing/checkout", new { plan = TenantPlan.Team });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StripeWebhook_UpdatesTenantPlanOnCheckoutCompleted()
    {
        await using var factory = new StripeWebhookTestFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var tenantId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Webhook Org",
                Slug = $"webhook-{Guid.NewGuid():N}"[..20],
                Plan = TenantPlan.Trial,
                Region = TenantRegion.US,
                TrialEndsAt = DateTime.UtcNow.AddDays(3),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = "cs_test_123",
                    customer = "cus_test_123",
                    subscription = "sub_test_123",
                    client_reference_id = tenantId.ToString(),
                    metadata = new Dictionary<string, string>
                    {
                        ["tenant_id"] = tenantId.ToString(),
                        ["plan"] = "Team"
                    }
                }
            }
        });

        using var client = factory.CreateClient();
        var response = await client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        var tenant = await verifyDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Plan.Should().Be(TenantPlan.Team);
        tenant.StripeCustomerId.Should().Be("cus_test_123");
        tenant.StripeSubscriptionId.Should().Be("sub_test_123");
        tenant.SubscriptionStatus.Should().Be(TenantSubscriptionStatus.Active);
    }

    [Fact]
    public void TenancyAdminPage_ShowsStripeUpgradeActions()
    {
        var page = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Admin/Tenancy.cshtml"));

        page.Should().Contain("Upgrade to Team");
        page.Should().Contain("Manage billing");
        page.Should().Contain("Stripe billing is not configured");
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

    private sealed class StripeWebhookTestFactory : TestWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?>? AdditionalSettings { get; } =
            new Dictionary<string, string?>
            {
                ["Stripe:Enabled"] = "true",
                ["Stripe:WebhookSecret"] = string.Empty
            };
    }
}
