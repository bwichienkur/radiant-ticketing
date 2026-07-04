using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class CodeTableReference : BaseEntity
{
    public Guid RepositoryId { get; set; }
    public CodeTableReferenceSourceType SourceType { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string? SourceMember { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public double ConfidenceScore { get; set; }

    public Repository Repository { get; set; } = null!;
}
