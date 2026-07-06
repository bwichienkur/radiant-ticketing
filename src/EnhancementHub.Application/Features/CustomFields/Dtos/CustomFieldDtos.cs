using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.CustomFields.Dtos;

public sealed record CustomFieldDefinitionDto(
    Guid Id,
    string Key,
    string Label,
    CustomFieldType FieldType,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<string> Options);

public sealed record CustomFieldValueInput(
    string Key,
    string? TextValue,
    double? NumberValue,
    DateTime? DateValue,
    Guid? UserValueId);

public sealed record CustomFieldValueDto(
    string Key,
    string Label,
    CustomFieldType FieldType,
    string? TextValue,
    double? NumberValue,
    DateTime? DateValue,
    Guid? UserValueId,
    string? UserDisplayName);
