using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IAuthenticationConfigurationService
{
    AuthenticationConfigurationStatusDto GetStatus();
}
