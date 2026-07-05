using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ApprovalPolicyEvaluator : IApprovalPolicyEvaluator
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ApprovalPolicyEvaluator(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<ApprovalPolicyEvaluationResult> EvaluateAsync(
        Guid enhancementRequestId,
        UserRole approverRole,
        CancellationToken cancellationToken = default)
    {
        if (approverRole == UserRole.Admin)
        {
            return new ApprovalPolicyEvaluationResult(true, null, null);
        }

        var request = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.Id == enhancementRequestId)
            .Select(r => new
            {
                r.Department,
                r.TargetApplicationId,
                LatestRisk = _dbContext.EnhancementAnalyses
                    .Where(a => a.EnhancementRequestId == r.Id)
                    .OrderByDescending(a => a.Version)
                    .Select(a => (RiskLevel?)a.RiskLevel)
                    .FirstOrDefault(),
                ApplicationTier = r.TargetApplicationId == null
                    ? (ApplicationTier?)null
                    : _dbContext.Applications
                        .Where(a => a.Id == r.TargetApplicationId)
                        .Select(a => (ApplicationTier?)a.Tier)
                        .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return new ApprovalPolicyEvaluationResult(true, null, null);
        }

        var rules = await _dbContext.ApprovalPolicyRules
            .AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (!RuleMatches(rule, request.LatestRisk, request.Department, request.ApplicationTier))
            {
                continue;
            }

            if (!RoleSatisfies(approverRole, rule.RequiredRole))
            {
                if (rule.BlockApproval)
                {
                    return new ApprovalPolicyEvaluationResult(
                        false,
                        rule.Name,
                        rule.Message);
                }
            }
        }

        return new ApprovalPolicyEvaluationResult(true, null, null);
    }

    internal static bool RuleMatches(
        Domain.Entities.ApprovalPolicyRule rule,
        RiskLevel? riskLevel,
        string? department,
        ApplicationTier? applicationTier)
    {
        if (rule.MinimumRiskLevel.HasValue)
        {
            if (!riskLevel.HasValue || riskLevel.Value < rule.MinimumRiskLevel.Value)
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.Department))
        {
            if (string.IsNullOrWhiteSpace(department)
                || !string.Equals(rule.Department, department, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (rule.ApplicationTier.HasValue)
        {
            if (!applicationTier.HasValue || applicationTier.Value != rule.ApplicationTier.Value)
            {
                return false;
            }
        }

        return rule.MinimumRiskLevel.HasValue
               || !string.IsNullOrWhiteSpace(rule.Department)
               || rule.ApplicationTier.HasValue;
    }

    internal static bool RoleSatisfies(UserRole actual, UserRole required) =>
        actual == UserRole.Admin || actual == required;
}
