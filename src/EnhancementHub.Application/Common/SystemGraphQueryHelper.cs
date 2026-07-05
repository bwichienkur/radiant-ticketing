using EnhancementHub.Application.Features.SystemIntelligence.Dtos;

namespace EnhancementHub.Application.Common;

public static class SystemGraphQueryHelper
{
    public sealed record GraphQueryResult(
        IReadOnlyList<SystemGraphNodeDto> Nodes,
        IReadOnlyList<SystemGraphEdgeDto> Edges,
        int TotalNodeCount,
        bool Truncated);

    public static GraphQueryResult Apply(
        IReadOnlyList<SystemGraphNodeDto> allNodes,
        IReadOnlyList<SystemGraphEdgeDto> allEdges,
        string? rootNodeId,
        int maxDepth,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        maxDepth = Math.Clamp(maxDepth, 1, 20);

        var nodeById = allNodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        var adjacency = BuildAdjacency(allEdges);

        HashSet<string> reachable;
        if (!string.IsNullOrWhiteSpace(rootNodeId) && nodeById.ContainsKey(rootNodeId))
        {
            reachable = CollectWithinDepth(rootNodeId, adjacency, maxDepth);
        }
        else
        {
            var roots = allNodes
                .Where(n => n.Type is "Application" or "Repository")
                .Select(n => n.Id)
                .ToList();
            if (roots.Count == 0)
            {
                roots = allNodes.Select(n => n.Id).Take(1).ToList();
            }

            reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots)
            {
                foreach (var id in CollectWithinDepth(root, adjacency, maxDepth))
                {
                    reachable.Add(id);
                }
            }
        }

        var filteredNodes = allNodes.Where(n => reachable.Contains(n.Id)).ToList();
        var totalNodeCount = filteredNodes.Count;
        var pagedNodes = filteredNodes
            .OrderBy(n => n.Type)
            .ThenBy(n => n.Label)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedIds = pagedNodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pagedEdges = allEdges
            .Where(e => pagedIds.Contains(e.FromId) && pagedIds.Contains(e.ToId))
            .ToList();

        var truncated = totalNodeCount > pagedNodes.Count
            || reachable.Count < allNodes.Count;

        return new GraphQueryResult(pagedNodes, pagedEdges, totalNodeCount, truncated);
    }

    private static Dictionary<string, HashSet<string>> BuildAdjacency(IReadOnlyList<SystemGraphEdgeDto> edges)
    {
        var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in edges)
        {
            AddNeighbor(adjacency, edge.FromId, edge.ToId);
            AddNeighbor(adjacency, edge.ToId, edge.FromId);
        }

        return adjacency;
    }

    private static void AddNeighbor(Dictionary<string, HashSet<string>> adjacency, string from, string to)
    {
        if (!adjacency.TryGetValue(from, out var neighbors))
        {
            neighbors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            adjacency[from] = neighbors;
        }

        neighbors.Add(to);
    }

    private static HashSet<string> CollectWithinDepth(
        string rootId,
        Dictionary<string, HashSet<string>> adjacency,
        int maxDepth)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootId };
        var queue = new Queue<(string Id, int Depth)>();
        queue.Enqueue((rootId, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (depth >= maxDepth || !adjacency.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue((neighbor, depth + 1));
                }
            }
        }

        return visited;
    }
}
