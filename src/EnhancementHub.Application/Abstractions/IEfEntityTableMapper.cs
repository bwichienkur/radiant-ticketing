using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IEfEntityTableMapper
{
    Task<IReadOnlyList<EntityMappingInfo>> MapEntitiesAsync(
        string rootPath,
        IReadOnlyList<string> dbContextTypes,
        CancellationToken cancellationToken = default);
}
