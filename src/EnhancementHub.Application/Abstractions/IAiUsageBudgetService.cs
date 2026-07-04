namespace EnhancementHub.Application.Abstractions;

public interface IAiUsageBudgetService
{
    Task EnsureWithinBudgetAsync(CancellationToken cancellationToken = default);
}
