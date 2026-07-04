using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class CodeEntityMapping : BaseEntity
{
    public Guid RepositoryId { get; set; }
    public string EntityClassName { get; set; } = string.Empty;
    public string EntityNamespace { get; set; } = string.Empty;
    public string EntityFilePath { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string? DbContextType { get; set; }
    public EntityMappingSource MappingSource { get; set; }
    public double ConfidenceScore { get; set; }

    public Repository Repository { get; set; } = null!;
}
