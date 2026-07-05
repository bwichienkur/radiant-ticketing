using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Policies.Dtos;
using EnhancementHub.Application.Features.Policies.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Policies.Queries;

public sealed record ListApprovalPolicyRulesQuery : IRequest<IReadOnlyList<ApprovalPolicyRuleDto>>;

public sealed class ListApprovalPolicyRulesQueryHandler
    : IRequestHandler<ListApprovalPolicyRulesQuery, IReadOnlyList<ApprovalPolicyRuleDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListApprovalPolicyRulesQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyList<ApprovalPolicyRuleDto>> Handle(
        ListApprovalPolicyRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await _dbContext.ApprovalPolicyRules
            .AsNoTracking()
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rules.Select(UpsertApprovalPolicyRuleCommandHandler.ToDto).ToList();
    }
}
