using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class SystemGraphEdge : BaseEntity
{
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }
    public GraphEdgeType EdgeType { get; set; }
    public string? Label { get; set; }
    public double ConfidenceScore { get; set; }
    public string? MetadataJson { get; set; }

    public SystemGraphNode SourceNode { get; set; } = null!;
    public SystemGraphNode TargetNode { get; set; } = null!;
}
