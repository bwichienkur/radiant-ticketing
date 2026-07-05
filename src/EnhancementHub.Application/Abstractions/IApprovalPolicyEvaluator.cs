using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IApprovalPolicyEvaluator
{
    Task<ApprovalPolicyEvaluationResult> EvaluateAsync(
        Guid enhancementRequestId,
        UserRole approverRole,
        CancellationToken cancellationToken = default);
}

public sealed record ApprovalPolicyEvaluationResult(
    bool Allowed,
    string? BlockedByRuleName,
    string? Message);
