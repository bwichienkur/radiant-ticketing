using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class SystemGraphNode : BaseEntity
{
    public Guid? ApplicationId { get; set; }
    public Guid? RepositoryId { get; set; }
    public GraphNodeType NodeType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ReferenceKey { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public Application? Application { get; set; }
    public Repository? Repository { get; set; }
    public ICollection<SystemGraphEdge> OutgoingEdges { get; set; } = new List<SystemGraphEdge>();
    public ICollection<SystemGraphEdge> IncomingEdges { get; set; } = new List<SystemGraphEdge>();
}
