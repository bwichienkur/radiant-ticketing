using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Features.Templates.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Templates.Queries;

public sealed record ListEnhancementTemplatesQuery(string? DomainCategory = null)
    : IRequest<IReadOnlyList<EnhancementTemplateSummaryDto>>;

public sealed class ListEnhancementTemplatesQueryHandler
    : IRequestHandler<ListEnhancementTemplatesQuery, IReadOnlyList<EnhancementTemplateSummaryDto>>
{
    private readonly IEnhancementRequestRepository _requests;

    public ListEnhancementTemplatesQueryHandler(IEnhancementRequestRepository requests) =>
        _requests = requests;

    public async Task<IReadOnlyList<EnhancementTemplateSummaryDto>> Handle(
        ListEnhancementTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _requests.ListActiveTemplatesAsync(request.DomainCategory, cancellationToken);
        return templates
            .Select(t => new EnhancementTemplateSummaryDto(
                t.Id,
                t.Name,
                t.DomainCategory,
                t.Title,
                t.Priority))
            .ToList();
    }
}

public sealed record GetEnhancementTemplateQuery(Guid Id) : IRequest<EnhancementTemplateDto>;

public sealed class GetEnhancementTemplateQueryHandler
    : IRequestHandler<GetEnhancementTemplateQuery, EnhancementTemplateDto>
{
    private readonly IEnhancementRequestRepository _requests;

    public GetEnhancementTemplateQueryHandler(IEnhancementRequestRepository requests) =>
        _requests = requests;

    public async Task<EnhancementTemplateDto> Handle(
        GetEnhancementTemplateQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _requests.GetTemplateByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Template {request.Id} was not found.");

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
