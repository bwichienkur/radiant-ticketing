using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Ai;

public sealed class AiUsageBudgetService : IAiUsageBudgetService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly AiOptions _options;

    public AiUsageBudgetService(IEnhancementHubDbContext dbContext, IOptions<AiOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task EnsureWithinBudgetAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Budget.Enabled)
        {
            return;
        }

        var dayStart = DateTime.UtcNow.Date;
        var runs = await _dbContext.AiPromptRuns
            .AsNoTracking()
            .Where(r => r.StartedAt >= dayStart)
            .Select(r => new { r.TotalTokens, r.EstimatedCostUsd })
            .ToListAsync(cancellationToken);

        var tokensUsed = runs.Sum(r => r.TotalTokens ?? 0);
        if (tokensUsed >= _options.Budget.DailyTokenLimit)
        {
            throw new AiBudgetExceededException(
                $"Daily AI token limit of {_options.Budget.DailyTokenLimit:N0} has been reached.");
        }

        var costUsed = runs.Sum(r => r.EstimatedCostUsd ?? 0m);
        if (costUsed >= _options.Budget.DailyCostLimitUsd)
        {
            throw new AiBudgetExceededException(
                $"Daily AI cost limit of ${_options.Budget.DailyCostLimitUsd:F2} has been reached.");
        }
    }

    public async Task<AiBudgetStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var dayStart = DateTime.UtcNow.Date;
        var runs = await _dbContext.AiPromptRuns
            .AsNoTracking()
            .Where(r => r.StartedAt >= dayStart)
            .Select(r => new { r.TotalTokens, r.EstimatedCostUsd })
            .ToListAsync(cancellationToken);

        var tokensUsed = runs.Sum(r => r.TotalTokens ?? 0);
        var costUsed = runs.Sum(r => r.EstimatedCostUsd ?? 0m);
        var tokenLimit = _options.Budget.DailyTokenLimit;
        var costLimit = _options.Budget.DailyCostLimitUsd;

        return new AiBudgetStatusDto(
            _options.Budget.Enabled,
            tokenLimit,
            tokensUsed,
            Math.Max(0, tokenLimit - tokensUsed),
            costLimit,
            costUsed,
            Math.Max(0m, costLimit - costUsed));
    }
}
