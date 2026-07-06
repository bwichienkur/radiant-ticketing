using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface IEnhancementRequestRepository
{
    Task<EnhancementRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EnhancementRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalAction>> ListApprovalActionsAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken = default);
    Task<EnhancementAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnhancementTemplate>> ListActiveTemplatesAsync(
        string? domainCategory,
        CancellationToken cancellationToken = default);
    Task<EnhancementTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomFieldDefinition>> ListCustomFieldDefinitionsAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnhancementRequestCustomFieldValue>> GetCustomFieldValuesAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
    void Add(EnhancementRequest request);
    void AddFeedback(ProductFeedback feedback);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
