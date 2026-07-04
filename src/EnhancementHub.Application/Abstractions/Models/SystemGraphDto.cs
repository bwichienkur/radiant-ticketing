using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed class SystemGraphDto
{
    public Guid? ApplicationId { get; set; }
    public Guid? RepositoryId { get; set; }
    public IReadOnlyList<GraphNodeDto> Nodes { get; set; } = Array.Empty<GraphNodeDto>();
    public IReadOnlyList<GraphEdgeDto> Edges { get; set; } = Array.Empty<GraphEdgeDto>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GraphNodeDto
{
    public Guid Id { get; set; }
    public GraphNodeType NodeType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ReferenceKey { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public sealed class GraphEdgeDto
{
    public Guid Id { get; set; }
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }
    public GraphEdgeType EdgeType { get; set; }
    public string? Label { get; set; }
    public double ConfidenceScore { get; set; }
}
