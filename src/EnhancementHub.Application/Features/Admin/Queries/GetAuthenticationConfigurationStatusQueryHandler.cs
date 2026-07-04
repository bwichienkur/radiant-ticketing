using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetAuthenticationConfigurationStatusQueryHandler
    : IRequestHandler<GetAuthenticationConfigurationStatusQuery, AuthenticationConfigurationStatusDto>
{
    private readonly IAuthenticationConfigurationService _configurationService;

    public GetAuthenticationConfigurationStatusQueryHandler(
        IAuthenticationConfigurationService configurationService) =>
        _configurationService = configurationService;

    public Task<AuthenticationConfigurationStatusDto> Handle(
        GetAuthenticationConfigurationStatusQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_configurationService.GetStatus());
}
