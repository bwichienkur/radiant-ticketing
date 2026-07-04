using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IRefactorBlastRadiusService
{
    Task<RefactorBlastRadiusResult> AnalyzeAsync(
        Guid applicationId,
        string targetTableOrEntity,
        CancellationToken cancellationToken = default);
}
