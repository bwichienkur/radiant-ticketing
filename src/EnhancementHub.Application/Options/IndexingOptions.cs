namespace EnhancementHub.Application.Options;

public sealed class IndexingOptions
{
    public const string SectionName = "Indexing";

    public bool IncrementalEnabled { get; set; } = true;

    public int MaxFilesPerRun { get; set; } = 5000;

    public int FreshnessSlaHours { get; set; } = 24;
}
