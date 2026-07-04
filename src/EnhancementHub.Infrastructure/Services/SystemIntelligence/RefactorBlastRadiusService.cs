using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class RefactorBlastRadiusService : IRefactorBlastRadiusService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public RefactorBlastRadiusService(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefactorBlastRadiusResult> AnalyzeAsync(
        Guid applicationId,
        string targetTableOrEntity,
        CancellationToken cancellationToken = default)
    {
        var normalizedTarget = targetTableOrEntity.Trim();
        var nodes = await _dbContext.SystemGraphNodes
            .Where(n => n.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
        {
            return new RefactorBlastRadiusResult { TargetName = normalizedTarget };
        }

        var edges = await _dbContext.SystemGraphEdges
            .Where(e => nodes.Select(n => n.Id).Contains(e.SourceNodeId)
                        || nodes.Select(n => n.Id).Contains(e.TargetNodeId))
            .ToListAsync(cancellationToken);

        var startNodes = nodes.Where(n =>
            n.Label.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase)
            || n.ReferenceKey.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (startNodes.Count == 0)
        {
            startNodes = nodes.Where(n =>
                n.Label.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>(startNodes.Select(n => n.Id));
        foreach (var id in startNodes.Select(n => n.Id))
        {
            visited.Add(id);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var relatedEdgeIds = edges
                .Where(e => e.SourceNodeId == currentId || e.TargetNodeId == currentId)
                .ToList();

            foreach (var edge in relatedEdgeIds)
            {
                var neighborId = edge.SourceNodeId == currentId ? edge.TargetNodeId : edge.SourceNodeId;
                if (visited.Add(neighborId))
                {
                    queue.Enqueue(neighborId);
                }
            }
        }

        var traversed = nodes.Where(n => visited.Contains(n.Id)).ToList();

        return new RefactorBlastRadiusResult
        {
            TargetName = normalizedTarget,
            AffectedTables = traversed.Where(n => n.NodeType == GraphNodeType.Table).Select(n => n.Label).Distinct().ToList(),
            AffectedEntities = traversed.Where(n => n.NodeType == GraphNodeType.Entity).Select(n => n.Label).Distinct().ToList(),
            AffectedServices = traversed.Where(n => n.NodeType == GraphNodeType.Service).Select(n => n.Label).Distinct().ToList(),
            AffectedApiEndpoints = traversed.Where(n => n.NodeType == GraphNodeType.ApiEndpoint).Select(n => n.Label).Distinct().ToList(),
            TraversedNodes = traversed.Select(n => new GraphNodeDto
            {
                Id = n.Id,
                NodeType = n.NodeType,
                Label = n.Label,
                ReferenceKey = n.ReferenceKey,
                MetadataJson = n.MetadataJson
            }).ToList()
        };
    }
}
