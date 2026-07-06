namespace EnhancementHub.Application.Abstractions;

public interface ISlaEscalationService
{
    Task<int> ProcessEscalationsAsync(CancellationToken cancellationToken = default);
}
