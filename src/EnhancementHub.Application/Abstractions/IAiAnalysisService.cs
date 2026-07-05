using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyzeEnhancementAsync(
        Guid enhancementRequestId,
        string title,
        string description,
        string? repositoryContext,
        string? applicationContext = null,
        CancellationToken cancellationToken = default);
}
