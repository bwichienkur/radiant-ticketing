namespace EnhancementHub.Domain.Enums;

public enum EnhancementRequestStatus
{
    Submitted,
    AiAnalyzing,
    NeedsClarification,
    PendingApproval,
    Approved,
    Rejected,
    ReadyForDevelopment,
    InProgress,
    Completed,
    Cancelled
}
