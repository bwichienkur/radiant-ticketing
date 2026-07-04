namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record DataRetentionStatusDto(
    bool Enabled,
    int AiPromptRunsRetentionDays,
    int AttachmentsRetentionDays,
    int BatchSize,
    int EligibleAiPromptRunCount,
    int EligibleAttachmentCount,
    DateTime? AiPromptRunsCutoffUtc,
    DateTime? AttachmentsCutoffUtc);

public sealed record DataRetentionResultDto(
    bool DryRun,
    int AiPromptRunsDeleted,
    int RetrievedContextItemsDeleted,
    int AttachmentsDeleted,
    int AttachmentFilesDeleted,
    DateTime AppliedAtUtc);
