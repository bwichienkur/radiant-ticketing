using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GraphEdgeEntity = EnhancementHub.Domain.Entities.SystemGraphEdge;
using GraphNodeEntity = EnhancementHub.Domain.Entities.SystemGraphNode;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SystemGraphBuilderService : ISystemGraphBuilder
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ILogger<SystemGraphBuilderService> _logger;

    public SystemGraphBuilderService(
        IEnhancementHubDbContext dbContext,
        ILogger<SystemGraphBuilderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SystemGraphDto> BuildForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .Include(a => a.Repositories)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        await ClearGraphAsync(applicationId, null, cancellationToken);

        var nodes = new List<GraphNodeEntity>();
        var edges = new List<GraphEdgeEntity>();
        var now = DateTime.UtcNow;

        var appNode = CreateNode(applicationId, null, GraphNodeType.Application, application.Name, $"app:{applicationId}", now);
        nodes.Add(appNode);

        foreach (var repo in application.Repositories)
        {
            var repoNode = CreateNode(applicationId, repo.Id, GraphNodeType.Repository, repo.Name, $"repo:{repo.Id}", now);
            nodes.Add(repoNode);
            edges.Add(CreateEdge(appNode, repoNode, GraphEdgeType.Contains, null, 1.0, now));

            await AddRepositoryNodesAsync(applicationId, repo, nodes, edges, now, cancellationToken);
        }

        await AddDatabaseNodesAsync(applicationId, nodes, edges, now, cancellationToken);

        _dbContext.SystemGraphNodes.AddRange(nodes);
        _dbContext.SystemGraphEdges.AddRange(edges);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Built system graph for application {ApplicationId}: {NodeCount} nodes, {EdgeCount} edges",
            applicationId, nodes.Count, edges.Count);

        return ToDto(applicationId, null, nodes, edges);
    }

    public async Task<SystemGraphDto> BuildForRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Repository {repositoryId} not found.");

        await ClearGraphAsync(repository.ApplicationId, repositoryId, cancellationToken);

        var nodes = new List<GraphNodeEntity>();
        var edges = new List<GraphEdgeEntity>();
        var now = DateTime.UtcNow;

        var repoNode = CreateNode(repository.ApplicationId, repositoryId, GraphNodeType.Repository, repository.Name, $"repo:{repositoryId}", now);
        nodes.Add(repoNode);

        await AddRepositoryNodesAsync(repository.ApplicationId, repository, nodes, edges, now, cancellationToken);

        _dbContext.SystemGraphNodes.AddRange(nodes);
        _dbContext.SystemGraphEdges.AddRange(edges);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(repository.ApplicationId, repositoryId, nodes, edges);
    }

    private async Task AddRepositoryNodesAsync(
        Guid applicationId,
        Repository repository,
        List<GraphNodeEntity> nodes,
        List<GraphEdgeEntity> edges,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var repoNode = nodes.First(n => n.RepositoryId == repository.Id && n.NodeType == GraphNodeType.Repository);

        var mappings = await _dbContext.CodeEntityMappings
            .Where(m => m.RepositoryId == repository.Id)
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            var entityNode = CreateNode(
                applicationId,
                repository.Id,
                GraphNodeType.Entity,
                mapping.EntityClassName,
                $"entity:{repository.Id}:{mapping.EntityClassName}",
                now);
            nodes.Add(entityNode);
            edges.Add(CreateEdge(repoNode, entityNode, GraphEdgeType.Contains, null, mapping.ConfidenceScore, now));

            if (!string.IsNullOrWhiteSpace(mapping.DbContextType))
            {
                var ctxNode = nodes.FirstOrDefault(n => n.ReferenceKey == $"dbctx:{mapping.DbContextType}")
                    ?? CreateNode(applicationId, repository.Id, GraphNodeType.DbContext, mapping.DbContextType, $"dbctx:{mapping.DbContextType}", now);
                if (!nodes.Contains(ctxNode))
                {
                    nodes.Add(ctxNode);
                    edges.Add(CreateEdge(repoNode, ctxNode, GraphEdgeType.Contains, null, 0.9, now));
                }

                edges.Add(CreateEdge(ctxNode, entityNode, GraphEdgeType.Contains, null, 0.85, now));
            }
        }

        var indexedFiles = await _dbContext.IndexedFiles
            .Where(f => f.RepositoryId == repository.Id)
            .ToListAsync(cancellationToken);

        foreach (var file in indexedFiles.Where(f => f.FilePath.Contains("Controller", StringComparison.OrdinalIgnoreCase)))
        {
            var endpointNode = CreateNode(
                applicationId,
                repository.Id,
                GraphNodeType.ApiEndpoint,
                Path.GetFileNameWithoutExtension(file.FilePath),
                $"api:{repository.Id}:{file.FilePath}",
                now);
            nodes.Add(endpointNode);
            edges.Add(CreateEdge(repoNode, endpointNode, GraphEdgeType.Contains, null, 0.75, now));
        }

        foreach (var file in indexedFiles.Where(f => f.FilePath.Contains("Service", StringComparison.OrdinalIgnoreCase)))
        {
            var serviceNode = CreateNode(
                applicationId,
                repository.Id,
                GraphNodeType.Service,
                Path.GetFileNameWithoutExtension(file.FilePath),
                $"svc:{repository.Id}:{file.FilePath}",
                now);
            nodes.Add(serviceNode);
            edges.Add(CreateEdge(repoNode, serviceNode, GraphEdgeType.Contains, null, 0.75, now));
        }
    }

    private async Task AddDatabaseNodesAsync(
        Guid applicationId,
        List<GraphNodeEntity> nodes,
        List<GraphEdgeEntity> edges,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var connections = await _dbContext.DatabaseConnections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .Where(c => c.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        foreach (var connection in connections)
        {
            foreach (var table in connection.Tables)
            {
                var tableNode = CreateNode(
                    applicationId,
                    null,
                    GraphNodeType.Table,
                    $"{table.SchemaName}.{table.TableName}",
                    $"table:{connection.Id}:{table.SchemaName}.{table.TableName}",
                    now);
                nodes.Add(tableNode);

                foreach (var column in table.Columns)
                {
                    var columnNode = CreateNode(
                        applicationId,
                        null,
                        GraphNodeType.Column,
                        column.Name,
                        $"column:{table.Id}:{column.Name}",
                        now);
                    nodes.Add(columnNode);
                    edges.Add(CreateEdge(tableNode, columnNode, GraphEdgeType.Contains, null, 1.0, now));
                }

                var entityMapping = await _dbContext.CodeEntityMappings
                    .Where(m => m.TableName == table.TableName && m.SchemaName == table.SchemaName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (entityMapping is not null)
                {
                    var entityNode = nodes.FirstOrDefault(n =>
                        n.NodeType == GraphNodeType.Entity
                        && n.Label == entityMapping.EntityClassName);
                    if (entityNode is not null)
                    {
                        edges.Add(CreateEdge(entityNode, tableNode, GraphEdgeType.MapsTo, null, entityMapping.ConfidenceScore, now));
                    }
                }
            }
        }

        var relationships = await _dbContext.DatabaseRelationships
            .Where(r => connections.Select(c => c.Id).Contains(r.DatabaseConnectionId))
            .ToListAsync(cancellationToken);

        foreach (var rel in relationships)
        {
            var fromTable = connections.SelectMany(c => c.Tables).FirstOrDefault(t => t.Id == rel.FromTableId);
            var toTable = connections.SelectMany(c => c.Tables).FirstOrDefault(t => t.Id == rel.ToTableId);
            if (fromTable is null || toTable is null)
            {
                continue;
            }

            var fromNode = nodes.FirstOrDefault(n => n.ReferenceKey == $"table:{rel.DatabaseConnectionId}:{fromTable.SchemaName}.{fromTable.TableName}");
            var toNode = nodes.FirstOrDefault(n => n.ReferenceKey == $"table:{rel.DatabaseConnectionId}:{toTable.SchemaName}.{toTable.TableName}");
            if (fromNode is not null && toNode is not null)
            {
                edges.Add(CreateEdge(fromNode, toNode, GraphEdgeType.ForeignKey, rel.FromColumnName, 1.0, now));
            }
        }
    }

    private async Task ClearGraphAsync(Guid? applicationId, Guid? repositoryId, CancellationToken cancellationToken)
    {
        var existingNodes = await _dbContext.SystemGraphNodes
            .Where(n => (applicationId == null || n.ApplicationId == applicationId)
                        && (repositoryId == null || n.RepositoryId == repositoryId))
            .ToListAsync(cancellationToken);

        if (existingNodes.Count == 0)
        {
            return;
        }

        var nodeIds = existingNodes.Select(n => n.Id).ToList();
        var existingEdges = await _dbContext.SystemGraphEdges
            .Where(e => nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(cancellationToken);

        _dbContext.SystemGraphEdges.RemoveRange(existingEdges);
        _dbContext.SystemGraphNodes.RemoveRange(existingNodes);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static GraphNodeEntity CreateNode(
        Guid? applicationId,
        Guid? repositoryId,
        GraphNodeType nodeType,
        string label,
        string referenceKey,
        DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        RepositoryId = repositoryId,
        NodeType = nodeType,
        Label = label,
        ReferenceKey = referenceKey,
        LastUpdatedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static GraphEdgeEntity CreateEdge(
        GraphNodeEntity source,
        GraphNodeEntity target,
        GraphEdgeType edgeType,
        string? label,
        double confidence,
        DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        SourceNodeId = source.Id,
        TargetNodeId = target.Id,
        EdgeType = edgeType,
        Label = label,
        ConfidenceScore = confidence,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static SystemGraphDto ToDto(Guid? applicationId, Guid? repositoryId, List<GraphNodeEntity> nodes, List<GraphEdgeEntity> edges) =>
        new()
        {
            ApplicationId = applicationId,
            RepositoryId = repositoryId,
            GeneratedAt = DateTime.UtcNow,
            Nodes = nodes.Select(n => new GraphNodeDto
            {
                Id = n.Id,
                NodeType = n.NodeType,
                Label = n.Label,
                ReferenceKey = n.ReferenceKey,
                MetadataJson = n.MetadataJson
            }).ToList(),
            Edges = edges.Select(e => new GraphEdgeDto
            {
                Id = e.Id,
                SourceNodeId = e.SourceNodeId,
                TargetNodeId = e.TargetNodeId,
                EdgeType = e.EdgeType,
                Label = e.Label,
                ConfidenceScore = e.ConfidenceScore
            }).ToList()
        };
}
