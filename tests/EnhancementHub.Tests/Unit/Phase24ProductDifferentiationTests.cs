using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase24ProductDifferentiationTests
{
    [Fact]
    public void ApprovalPolicy_MatchesCriticalRisk()
    {
        var rule = new ApprovalPolicyRule { MinimumRiskLevel = RiskLevel.Critical };
        ApprovalPolicyEvaluator.RuleMatches(rule, RiskLevel.Critical, null, null).Should().BeTrue();
        ApprovalPolicyEvaluator.RuleMatches(rule, RiskLevel.Medium, null, null).Should().BeFalse();
    }

    [Fact]
    public void ApprovalPolicy_MatchesDepartment()
    {
        var rule = new ApprovalPolicyRule { Department = "Finance" };
        ApprovalPolicyEvaluator.RuleMatches(rule, null, "Finance", null).Should().BeTrue();
        ApprovalPolicyEvaluator.RuleMatches(rule, null, "Engineering", null).Should().BeFalse();
    }

    [Fact]
    public void ApprovalPolicy_MatchesApplicationTier()
    {
        var rule = new ApprovalPolicyRule { ApplicationTier = ApplicationTier.Critical };
        ApprovalPolicyEvaluator.RuleMatches(rule, null, null, ApplicationTier.Critical).Should().BeTrue();
        ApprovalPolicyEvaluator.RuleMatches(rule, null, null, ApplicationTier.Standard).Should().BeFalse();
    }

    [Fact]
    public void ApprovalPolicy_AdminRoleAlwaysSatisfies()
    {
        ApprovalPolicyEvaluator.RoleSatisfies(UserRole.Admin, UserRole.Approver).Should().BeTrue();
        ApprovalPolicyEvaluator.RoleSatisfies(UserRole.Approver, UserRole.Admin).Should().BeFalse();
    }

    [Fact]
    public void AnalysisComparison_DetectsFieldChanges()
    {
        var a = new EnhancementAnalysis
        {
            FeatureSummary = "Original summary",
            TechnicalRequirements = "Same",
            TestingPlan = "Plan A"
        };
        var b = new EnhancementAnalysis
        {
            FeatureSummary = "Architect refined summary",
            TechnicalRequirements = "Same",
            TestingPlan = "Plan B"
        };

        var changes = Application.Features.Analysis.Queries.GetAnalysisComparisonQueryHandler
            .CompareFields(a, b);

        changes.Should().Contain(c => c.FieldName == "FeatureSummary" && c.Changed);
        changes.Should().Contain(c => c.FieldName == "TestingPlan" && c.Changed);
        changes.Should().Contain(c => c.FieldName == "TechnicalRequirements" && !c.Changed);
    }

    [Fact]
    public async Task RoiReportEndpoint_RequiresAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/reporting/roi");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TemplatesEndpoint_ReturnsSeededTemplates()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        var builder = factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(UserRole.Developer, email: "template-user@test.com");
        using var client = await factory.CreateAuthenticatedClientAsync(user);

        var response = await client.GetAsync("/api/templates");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Security");
    }

    [Fact]
    public async Task PolicyBlocksCriticalRiskApprovalForApprover()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        var builder = factory.CreateDataBuilder();
        var approver = await builder.CreateUserAsync(UserRole.Approver, email: "approver@test.com");
        var request = await builder.CreateEnhancementRequestAsync(
            approver,
            EnhancementRequestStatus.PendingApproval);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHub.Infrastructure.Persistence.EnhancementHubDbContext>();
            db.EnhancementAnalyses.Add(new EnhancementAnalysis
            {
                Id = Guid.NewGuid(),
                EnhancementRequestId = request.Id,
                Version = 1,
                RiskLevel = RiskLevel.Critical,
                ConfidenceScore = 0.9,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(approver);
        var response = await client.PostAsJsonAsync($"/api/approvals/{request.Id}/actions", new
        {
            actionType = ApprovalActionType.Approve,
            comments = "Attempt approve"
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }
}
