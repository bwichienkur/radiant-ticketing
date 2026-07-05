namespace EnhancementHub.Infrastructure.Options;

public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    public bool Enabled { get; set; }

    public int AiPromptRunsDays { get; set; } = 365;

    public int AttachmentsDays { get; set; } = 180;

    public int BatchSize { get; set; } = 500;

    public bool ArchiveAiPromptRunsBeforeDelete { get; set; }

    public string? ArchivePath { get; set; }
}
