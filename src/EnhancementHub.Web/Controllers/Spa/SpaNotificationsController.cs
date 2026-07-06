using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/notifications")]
public sealed class SpaNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public SpaNotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        return Ok(await _notificationService.ListForUserAsync(userId, unreadOnly, limit, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        return Ok(new { count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken) });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAsReadAsync(userId, id, cancellationToken);
        return Ok();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        return Ok(await _notificationService.GetPreferencesAsync(userId, cancellationToken));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] IReadOnlyList<UpdateNotificationPreferenceInput> preferences,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        await _notificationService.UpdatePreferencesAsync(userId, preferences, cancellationToken);
        return Ok(await _notificationService.GetPreferencesAsync(userId, cancellationToken));
    }
}
