using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, DateTime? expires = null);
}
