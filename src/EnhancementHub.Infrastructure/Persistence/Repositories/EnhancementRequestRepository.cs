using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class EnhancementRequestRepository : IEnhancementRequestRepository
{
    private readonly EnhancementHubDbContext _dbContext;

    public EnhancementRequestRepository(EnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public Task<EnhancementRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.EnhancementRequests
            .AsNoTracking()
            .Include(r => r.TargetApplication)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.Attachments)
            .Include(r => r.Comments).ThenInclude(c => c.User)
            .Include(r => r.Analyses)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<EnhancementRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.EnhancementRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApprovalAction>> ListApprovalActionsAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ApprovalActions
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.EnhancementRequestId == enhancementRequestId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<EnhancementAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
        _dbContext.EnhancementAttachments
            .AsNoTracking()
            .Include(a => a.EnhancementRequest)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

    public async Task<IReadOnlyList<EnhancementTemplate>> ListActiveTemplatesAsync(
        string? domainCategory,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EnhancementTemplates.AsNoTracking().Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(domainCategory))
        {
            query = query.Where(t => t.DomainCategory == domainCategory);
        }

        return await query
            .OrderBy(t => t.DomainCategory)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<EnhancementTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.EnhancementTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomFieldDefinition>> ListCustomFieldDefinitionsAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CustomFieldDefinitions.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Label)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnhancementRequestCustomFieldValue>> GetCustomFieldValuesAsync(
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.EnhancementRequestCustomFieldValues
            .AsNoTracking()
            .Include(v => v.Definition)
            .Include(v => v.UserValue)
            .Where(v => v.EnhancementRequestId == requestId)
            .OrderBy(v => v.Definition.SortOrder)
            .ToListAsync(cancellationToken);

    public void Add(EnhancementRequest request) => _dbContext.EnhancementRequests.Add(request);

    public void AddFeedback(ProductFeedback feedback) => _dbContext.ProductFeedbacks.Add(feedback);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
