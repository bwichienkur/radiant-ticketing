using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IDocumentationExportService
{
    Task<DocumentationBundle> ExportAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
