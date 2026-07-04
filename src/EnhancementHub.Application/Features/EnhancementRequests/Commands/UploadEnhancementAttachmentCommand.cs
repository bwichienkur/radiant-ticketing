using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Commands;

public sealed record UploadEnhancementAttachmentCommand(
    Guid EnhancementRequestId,
    string FileName,
    string ContentType,
    Stream Content) : IRequest<EnhancementAttachmentDto>;

public sealed class UploadEnhancementAttachmentCommandValidator : AbstractValidator<UploadEnhancementAttachmentCommand>
{
    public UploadEnhancementAttachmentCommandValidator()
    {
        RuleFor(x => x.EnhancementRequestId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class UploadEnhancementAttachmentCommandHandler
    : IRequestHandler<UploadEnhancementAttachmentCommand, EnhancementAttachmentDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IAttachmentScanService _attachmentScan;
    private readonly ICurrentUserService _currentUser;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly IAuditService _auditService;

    public UploadEnhancementAttachmentCommandHandler(
        IEnhancementHubDbContext dbContext,
        IFileStorageService fileStorage,
        IAttachmentScanService attachmentScan,
        ICurrentUserService currentUser,
        IEnhancementRequestAccessService accessService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _attachmentScan = attachmentScan;
        _currentUser = currentUser;
        _accessService = accessService;
        _auditService = auditService;
    }

    public async Task<EnhancementAttachmentDto> Handle(
        UploadEnhancementAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var enhancementRequest = await _accessService.GetAccessibleRequestAsync(
            request.EnhancementRequestId,
            cancellationToken);

        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to upload attachments.");
        }

        var scanResult = await _attachmentScan.ScanAsync(
            request.FileName,
            request.ContentType,
            request.Content,
            cancellationToken);

        if (!scanResult.IsAllowed)
        {
            throw new InvalidOperationException(scanResult.Details ?? "Attachment rejected by security scan.");
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var storagePath = await _fileStorage.SaveAsync(
            $"requests/{request.EnhancementRequestId:N}",
            request.FileName,
            request.Content,
            request.ContentType,
            cancellationToken);

        var now = DateTime.UtcNow;
        var attachment = new EnhancementAttachment
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.EnhancementRequestId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            StoragePath = storagePath,
            UploadedByUserId = _currentUser.UserId.Value,
            ScanStatus = scanResult.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase)
                ? Domain.Enums.AttachmentScanStatus.Skipped
                : Domain.Enums.AttachmentScanStatus.Clean,
            ScanDetails = scanResult.Details,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.EnhancementAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "AttachmentUploaded",
            nameof(EnhancementAttachment),
            attachment.Id,
            $"Uploaded attachment '{attachment.FileName}' to request {enhancementRequest.Title}.",
            cancellationToken);

        return new EnhancementAttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.CreatedAt);
    }
}
