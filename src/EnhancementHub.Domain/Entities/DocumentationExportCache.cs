using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DocumentationExportCache : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string MarkdownDocumentation { get; set; } = string.Empty;
    public string MermaidErd { get; set; } = string.Empty;
    public string SourceFingerprint { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Application Application { get; set; } = null!;
}
