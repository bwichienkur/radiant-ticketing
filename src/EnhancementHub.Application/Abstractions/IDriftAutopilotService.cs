namespace EnhancementHub.Application.Abstractions;

public interface IDriftAutopilotService
{
    Task<int> AutoDraftRequestsFromDriftAsync(CancellationToken cancellationToken = default);
}
