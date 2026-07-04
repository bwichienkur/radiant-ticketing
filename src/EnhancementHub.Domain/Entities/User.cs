using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<EnhancementRequest> SubmittedRequests { get; set; } = new List<EnhancementRequest>();
    public ICollection<EnhancementAttachment> UploadedAttachments { get; set; } = new List<EnhancementAttachment>();
    public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ExternalTicket> CreatedExternalTickets { get; set; } = new List<ExternalTicket>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
