using EnhancementHub.Application.Features.Branding.Commands;
using EnhancementHub.Application.Features.Branding.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/branding")]
public sealed class SpaBrandingController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaBrandingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("appearance")]
    public async Task<IActionResult> GetAppearance(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetUserAppearanceQuery(), cancellationToken));

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme(
        [FromBody] SpaUpdateThemeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new UpdateThemePreferenceCommand(request.ThemePreference), cancellationToken));

    [HttpPut("tenant")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTenantBranding(
        [FromBody] SpaUpdateTenantBrandingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpdateTenantBrandingCommand(request.LogoUrl, request.AccentColor, request.ProductName),
            cancellationToken));
}

public sealed record SpaUpdateThemeRequest(string ThemePreference);

public sealed record SpaUpdateTenantBrandingRequest(
    string? LogoUrl,
    string AccentColor,
    string? ProductName);
