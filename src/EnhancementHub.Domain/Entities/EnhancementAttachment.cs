using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class EnhancementAttachment : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public AttachmentScanStatus ScanStatus { get; set; } = AttachmentScanStatus.Pending;
    public string? ScanDetails { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
