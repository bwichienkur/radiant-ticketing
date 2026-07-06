namespace EnhancementHub.Application.Features.Search.Dtos;

public sealed record GlobalSearchItemDto(
    string Type,
    string Title,
    string? Subtitle,
    string Url,
    float Score = 0);

public sealed record GlobalSearchResultDto(
    string Query,
    IReadOnlyList<GlobalSearchItemDto> Items,
    IReadOnlyDictionary<string, IReadOnlyList<GlobalSearchItemDto>> Groups,
    string? SemanticHint = null);
