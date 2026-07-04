using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class AiPromptConfiguration : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SystemPromptTemplate { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? OutputSchema { get; set; }
}
