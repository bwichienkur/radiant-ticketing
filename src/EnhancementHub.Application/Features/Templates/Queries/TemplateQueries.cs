using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Templates.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Templates.Queries;

public sealed record ListEnhancementTemplatesQuery(string? DomainCategory = null)
    : IRequest<IReadOnlyList<EnhancementTemplateSummaryDto>>;

public sealed class ListEnhancementTemplatesQueryHandler
    : IRequestHandler<ListEnhancementTemplatesQuery, IReadOnlyList<EnhancementTemplateSummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListEnhancementTemplatesQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyList<EnhancementTemplateSummaryDto>> Handle(
        ListEnhancementTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.EnhancementTemplates
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(request.DomainCategory))
        {
            query = query.Where(t => t.DomainCategory == request.DomainCategory);
        }

        return await query
            .OrderBy(t => t.DomainCategory)
            .ThenBy(t => t.Name)
            .Select(t => new EnhancementTemplateSummaryDto(
                t.Id,
                t.Name,
                t.DomainCategory,
                t.Title,
                t.Priority))
            .ToListAsync(cancellationToken);
    }
}

public sealed record GetEnhancementTemplateQuery(Guid Id) : IRequest<EnhancementTemplateDto>;

public sealed class GetEnhancementTemplateQueryHandler
    : IRequestHandler<GetEnhancementTemplateQuery, EnhancementTemplateDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetEnhancementTemplateQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<EnhancementTemplateDto> Handle(
        GetEnhancementTemplateQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.EnhancementTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.IsActive, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException("EnhancementTemplate", request.Id);

        return new EnhancementTemplateDto(
            template.Id,
            template.Name,
            template.DomainCategory,
            template.Title,
            template.BusinessDescription,
            template.DesiredOutcome,
            template.Priority,
            template.SupportingNotes,
            template.IsActive);
    }
}
