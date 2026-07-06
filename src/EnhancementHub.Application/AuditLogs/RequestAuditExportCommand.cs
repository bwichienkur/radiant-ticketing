using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.AuditLogs;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Application.AuditLogs;

public sealed record RequestAuditExportCommand(
    string Format,
    DateTime? From = null,
    DateTime? To = null,
    string? EntityType = null,
    string? Action = null,
    Guid? UserId = null,
    int Limit = 10000) : IRequest<AuditExportSignedUrlResult>;

public sealed record AuditExportSignedUrlResult(
    string DownloadUrl,
    DateTime ExpiresAtUtc,
    int RecordCount,
    string Format,
    string FileName);

public sealed class RequestAuditExportCommandHandler
    : IRequestHandler<RequestAuditExportCommand, AuditExportSignedUrlResult>
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditExportTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public RequestAuditExportCommandHandler(
        IMediator mediator,
        IFileStorageService fileStorage,
        IAuditExportTokenService tokenService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<AuditExportSignedUrlResult> Handle(
        RequestAuditExportCommand request,
        CancellationToken cancellationToken)
    {
        var export = await _mediator.Send(
            new ExportAuditLogsQuery(
                request.Format,
                request.EntityType,
                request.Action,
                request.UserId,
                request.From,
                request.To,
                request.Limit),
            cancellationToken);

        await using var stream = new MemoryStream(export.Content);
        var storagePath = await _fileStorage.SaveAsync(
            "audit-exports",
            export.FileName,
            stream,
            export.ContentType,
            cancellationToken);

        var validityMinutes = int.TryParse(_configuration["AuditExport:UrlValidityMinutes"], out var minutes)
            ? minutes
            : 60;
        var validity = TimeSpan.FromMinutes(validityMinutes);
        var expiresAt = DateTime.UtcNow.Add(validity);
        var token = _tokenService.CreateToken(storagePath, expiresAt);

        var apiBase = _configuration["AuditExport:PublicApiBaseUrl"]
            ?? _configuration["Cors:WebOrigin"]
            ?? "http://localhost:5075";
        apiBase = apiBase.TrimEnd('/');

        var downloadUrl = $"{apiBase}/api/audit/download?token={Uri.EscapeDataString(token)}";

        return new AuditExportSignedUrlResult(
            downloadUrl,
            expiresAt,
            export.RecordCount,
            request.Format,
            export.FileName);
    }
}
