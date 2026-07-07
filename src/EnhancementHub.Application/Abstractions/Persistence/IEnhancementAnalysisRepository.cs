using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface IEnhancementAnalysisRepository
{
    Task<EnhancementAnalysis?> GetByRequestAsync(
        Guid enhancementRequestId,
        int? version,
        CancellationToken cancellationToken = default);
}
