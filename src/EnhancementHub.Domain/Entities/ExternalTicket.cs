using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class ExternalTicket : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public ExternalTicketProvider Provider { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Metadata { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
