namespace EnhancementHub.Application.Abstractions;

public interface ISystemIntelligenceFingerprintService
{
    Task<string> ComputeApplicationFingerprintAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<bool> IsDriftScanStaleAsync(Guid connectionId, CancellationToken cancellationToken = default);
}
