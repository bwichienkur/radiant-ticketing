using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record TeamSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    int ApplicationCount);

public sealed record TeamMemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    UserRole GlobalRole,
    string TeamRole,
    bool IsActive);

public sealed record TeamDetailDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<TeamMemberDto> Members,
    IReadOnlyList<TeamApplicationSummaryDto> Applications);

public sealed record TeamApplicationSummaryDto(Guid Id, string Name);

public sealed record AddTeamMemberResultDto(
    Guid TeamMemberId,
    Guid UserId,
    string Email,
    bool UserCreated,
    string? TemporaryPassword);

public static class TeamMemberRoles
{
    public const string Owner = "Owner";
    public const string Lead = "Lead";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All = [Owner, Lead, Member];
}
