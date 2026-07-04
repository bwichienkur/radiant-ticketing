using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Repositories.Dtos;

public sealed record RepositoryDto(
    Guid Id,
    Guid ApplicationId,
    string? ApplicationName,
    string Name,
    string Url,
    ExternalTicketProvider Provider,
    string DefaultBranch,
    DateTime? LastIndexedAt,
    IndexingStatus IndexingStatus);

public sealed record RepositoryStatusDto(
    Guid Id,
    string Name,
    IndexingStatus IndexingStatus,
    DateTime? LastIndexedAt,
    int IndexedFileCount);
