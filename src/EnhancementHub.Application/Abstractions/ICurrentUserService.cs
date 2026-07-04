using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
