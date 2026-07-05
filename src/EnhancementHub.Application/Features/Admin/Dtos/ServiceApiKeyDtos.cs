using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record ServiceApiKeySummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string KeyPrefix,
    UserRole Role,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime CreatedAt);

public sealed record CreateServiceApiKeyResultDto(
    Guid Id,
    string Name,
    string ApiKey,
    string KeyPrefix,
    UserRole Role,
    DateTime? ExpiresAt);
