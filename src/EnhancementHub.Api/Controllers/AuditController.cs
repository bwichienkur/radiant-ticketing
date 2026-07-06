using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.AuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditExportTokenService _tokenService;

    public AuditController(
        IMediator mediator,
        IFileStorageService fileStorage,
        IAuditExportTokenService tokenService)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
        _tokenService = tokenService;
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestExport(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] int limit = 10000,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(
            new RequestAuditExportCommand(format, from, to, entityType, action, userId, limit),
            cancellationToken));

    [HttpGet("download")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokenService.TryValidateToken(token, out var storagePath))
        {
            return Unauthorized(new { message = "Invalid or expired download token." });
        }

        var stream = await _fileStorage.OpenReadAsync(storagePath, cancellationToken);
        var fileName = Path.GetFileName(storagePath);
        var contentType = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "text/csv";

        return File(stream, contentType, fileName);
    }
}
