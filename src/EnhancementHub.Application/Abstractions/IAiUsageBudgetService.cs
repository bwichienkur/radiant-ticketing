namespace EnhancementHub.Application.Abstractions;

public interface IAiUsageBudgetService
{
    Task EnsureWithinBudgetAsync(CancellationToken cancellationToken = default);

    Task<AiBudgetStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
