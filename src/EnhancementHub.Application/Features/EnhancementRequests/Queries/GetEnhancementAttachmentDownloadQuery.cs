using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Queries;

public sealed record GetEnhancementAttachmentDownloadQuery(
    Guid EnhancementRequestId,
    Guid AttachmentId) : IRequest<EnhancementAttachmentDownloadResult>;

public sealed record EnhancementAttachmentDownloadResult(
    string FileName,
    string ContentType,
    string? PresignedDownloadUrl,
    Stream? ContentStream);

public sealed class GetEnhancementAttachmentDownloadQueryHandler
    : IRequestHandler<GetEnhancementAttachmentDownloadQuery, EnhancementAttachmentDownloadResult>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly TimeSpan _presignedUrlValidity = TimeSpan.FromMinutes(60);

    public GetEnhancementAttachmentDownloadQueryHandler(
        IEnhancementHubDbContext dbContext,
        IFileStorageService fileStorage,
        IEnhancementRequestAccessService accessService)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _accessService = accessService;
    }

    public async Task<EnhancementAttachmentDownloadResult> Handle(
        GetEnhancementAttachmentDownloadQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.GetAccessibleRequestAsync(request.EnhancementRequestId, cancellationToken);

        var attachment = await _dbContext.EnhancementAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == request.AttachmentId && a.EnhancementRequestId == request.EnhancementRequestId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementAttachment), request.AttachmentId);

        var presignedUrl = await _fileStorage.GetPresignedDownloadUrlAsync(
            attachment.StoragePath,
            _presignedUrlValidity,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(presignedUrl))
        {
            return new EnhancementAttachmentDownloadResult(
                attachment.FileName,
                attachment.ContentType,
                presignedUrl,
                null);
        }

        var stream = await _fileStorage.OpenReadAsync(attachment.StoragePath, cancellationToken);
        return new EnhancementAttachmentDownloadResult(
            attachment.FileName,
            attachment.ContentType,
            null,
            stream);
    }
}
