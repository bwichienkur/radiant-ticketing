namespace EnhancementHub.Application.Abstractions;

public sealed record AiBudgetStatusDto(
    bool Enabled,
    int DailyTokenLimit,
    int TokensUsedToday,
    int TokensRemaining,
    decimal DailyCostLimitUsd,
    decimal CostUsedTodayUsd,
    decimal CostRemainingUsd);
