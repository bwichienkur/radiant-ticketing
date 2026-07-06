using System.Text.Json;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;

namespace EnhancementHub.Application.Features.CustomFields.Queries;

public sealed record ListCustomFieldDefinitionsQuery(bool ActiveOnly = true)
    : IRequest<IReadOnlyList<CustomFieldDefinitionDto>>;

public sealed class ListCustomFieldDefinitionsQueryHandler
    : IRequestHandler<ListCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>
{
    private readonly IEnhancementRequestRepository _requests;

    public ListCustomFieldDefinitionsQueryHandler(IEnhancementRequestRepository requests) =>
        _requests = requests;

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> Handle(
        ListCustomFieldDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _requests.ListCustomFieldDefinitionsAsync(request.ActiveOnly, cancellationToken);
        return definitions.Select(CustomFieldQueries.ToDto).ToList();
    }
}

public sealed record GetRequestCustomFieldValuesQuery(Guid RequestId)
    : IRequest<IReadOnlyList<CustomFieldValueDto>>;

public sealed class GetRequestCustomFieldValuesQueryHandler
    : IRequestHandler<GetRequestCustomFieldValuesQuery, IReadOnlyList<CustomFieldValueDto>>
{
    private readonly IEnhancementRequestRepository _requests;

    public GetRequestCustomFieldValuesQueryHandler(IEnhancementRequestRepository requests) =>
        _requests = requests;

    public async Task<IReadOnlyList<CustomFieldValueDto>> Handle(
        GetRequestCustomFieldValuesQuery request,
        CancellationToken cancellationToken)
    {
        var values = await _requests.GetCustomFieldValuesAsync(request.RequestId, cancellationToken);
        return values
            .Select(v => new CustomFieldValueDto(
                v.Definition.Key,
                v.Definition.Label,
                v.Definition.FieldType,
                v.TextValue,
                v.NumberValue,
                v.DateValue,
                v.UserValueId,
                v.UserValue?.DisplayName))
            .ToList();
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
