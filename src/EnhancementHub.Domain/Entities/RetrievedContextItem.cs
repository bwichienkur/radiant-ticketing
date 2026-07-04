using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class RetrievedContextItem : BaseEntity
{
    public Guid AiPromptRunId { get; set; }
    public Guid? IndexedFileId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public double RelevanceScore { get; set; }

    public AiPromptRun AiPromptRun { get; set; } = null!;
    public IndexedFile? IndexedFile { get; set; }
}
