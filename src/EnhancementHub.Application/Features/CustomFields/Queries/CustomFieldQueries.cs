using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.CustomFields.Queries;

public sealed record ListCustomFieldDefinitionsQuery(bool ActiveOnly = true)
    : IRequest<IReadOnlyList<CustomFieldDefinitionDto>>;

public sealed class ListCustomFieldDefinitionsQueryHandler
    : IRequestHandler<ListCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListCustomFieldDefinitionsQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> Handle(
        ListCustomFieldDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CustomFieldDefinitions.AsNoTracking().AsQueryable();
        if (request.ActiveOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        var definitions = await query
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Label)
            .ToListAsync(cancellationToken);

        return definitions.Select(CustomFieldQueries.ToDto).ToList();
    }
}

public sealed record GetRequestCustomFieldValuesQuery(Guid RequestId)
    : IRequest<IReadOnlyList<CustomFieldValueDto>>;

public sealed class GetRequestCustomFieldValuesQueryHandler
    : IRequestHandler<GetRequestCustomFieldValuesQuery, IReadOnlyList<CustomFieldValueDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetRequestCustomFieldValuesQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyList<CustomFieldValueDto>> Handle(
        GetRequestCustomFieldValuesQuery request,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EnhancementRequestCustomFieldValues
            .AsNoTracking()
            .Include(v => v.Definition)
            .Include(v => v.UserValue)
            .Where(v => v.EnhancementRequestId == request.RequestId)
            .OrderBy(v => v.Definition.SortOrder)
            .Select(v => new CustomFieldValueDto(
                v.Definition.Key,
                v.Definition.Label,
                v.Definition.FieldType,
                v.TextValue,
                v.NumberValue,
                v.DateValue,
                v.UserValueId,
                v.UserValue != null ? v.UserValue.DisplayName : null))
            .ToListAsync(cancellationToken);
    }
}

internal static class CustomFieldQueries
{
    internal static CustomFieldDefinitionDto ToDto(CustomFieldDefinition entity)
    {
        IReadOnlyList<string> options = [];
        if (!string.IsNullOrWhiteSpace(entity.OptionsJson))
        {
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(entity.OptionsJson) ?? [];
            }
            catch (JsonException)
            {
                options = [];
            }
        }

        return new CustomFieldDefinitionDto(
            entity.Id,
            entity.Key,
            entity.Label,
            entity.FieldType,
            entity.IsRequired,
            entity.IsActive,
            entity.SortOrder,
            options);
    }
}
