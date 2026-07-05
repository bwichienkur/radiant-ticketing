using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GraphEdgeEntity = EnhancementHub.Domain.Entities.SystemGraphEdge;
using GraphNodeEntity = EnhancementHub.Domain.Entities.SystemGraphNode;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SystemGraphBuilderService : ISystemGraphBuilder
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly SystemIntelligenceOptions _options;
    private readonly ILogger<SystemGraphBuilderService> _logger;

    public SystemGraphBuilderService(
        IEnhancementHubDbContext dbContext,
        IOptions<SystemIntelligenceOptions> options,
        ILogger<SystemGraphBuilderService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SystemGraphDto> BuildForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .Include(a => a.Repositories)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        if (_options.IncrementalGraphEnabled)
        {
            var lastBuilt = await _dbContext.SystemGraphNodes
                .Where(n => n.ApplicationId == applicationId)
                .MaxAsync(n => (DateTime?)n.LastUpdatedAt, cancellationToken);

            if (lastBuilt.HasValue)
            {
                var staleRepos = application.Repositories
                    .Where(r => !r.LastIndexedAt.HasValue || r.LastIndexedAt > lastBuilt)
                    .ToList();

                var dbStale = await _dbContext.DatabaseConnections
                    .AnyAsync(
                        c => c.ApplicationId == applicationId
                             && c.LastScannedAt.HasValue
                             && c.LastScannedAt > lastBuilt,
                        cancellationToken);

                if (staleRepos.Count == 0 && !dbStale)
                {
                    _logger.LogInformation(
                        "System graph for application {ApplicationId} is current; skipping rebuild",
                        applicationId);
                    return await LoadGraphDtoAsync(applicationId, null, cancellationToken);
                }

                await EnsureApplicationShellAsync(application, cancellationToken);

                foreach (var repo in staleRepos)
                {
                    await RebuildRepositorySubgraphAsync(applicationId, repo, cancellationToken);
                }

                if (dbStale)
                {
                    await RebuildDatabaseSubgraphAsync(applicationId, cancellationToken);
                }

                await SaveSnapshotAsync(applicationId, cancellationToken);
                return await LoadGraphDtoAsync(applicationId, null, cancellationToken);
            }
        }

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

        _logger.LogInformation(
            "Built system graph for application {ApplicationId}: {NodeCount} nodes, {EdgeCount} edges",
            applicationId,
            nodes.Count,
            edges.Count);

        await SaveSnapshotAsync(applicationId, cancellationToken);
        return ToDto(applicationId, null, nodes, edges);
    }

    public async Task<SystemGraphDto> BuildForRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Repository {repositoryId} not found.");

        await RebuildRepositorySubgraphAsync(repository.ApplicationId, repository, cancellationToken);
        await SaveSnapshotAsync(repository.ApplicationId, cancellationToken);
        return await LoadGraphDtoAsync(repository.ApplicationId, repositoryId, cancellationToken);
    }

    private async Task EnsureApplicationShellAsync(Domain.Entities.Application application, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var applicationId = application.Id;

        var appNode = await _dbContext.SystemGraphNodes
            .FirstOrDefaultAsync(
                n => n.ApplicationId == applicationId && n.NodeType == GraphNodeType.Application,
                cancellationToken);

        if (appNode is null)
        {
            appNode = CreateNode(applicationId, null, GraphNodeType.Application, application.Name, $"app:{applicationId}", now);
            _dbContext.SystemGraphNodes.Add(appNode);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var repo in application.Repositories)
        {
            var repoNode = await _dbContext.SystemGraphNodes
                .FirstOrDefaultAsync(
                    n => n.RepositoryId == repo.Id && n.NodeType == GraphNodeType.Repository,
                    cancellationToken);

            if (repoNode is null)
            {
                repoNode = CreateNode(applicationId, repo.Id, GraphNodeType.Repository, repo.Name, $"repo:{repo.Id}", now);
                _dbContext.SystemGraphNodes.Add(repoNode);
                _dbContext.SystemGraphEdges.Add(CreateEdge(appNode, repoNode, GraphEdgeType.Contains, null, 1.0, now));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RebuildRepositorySubgraphAsync(
        Guid applicationId,
        Repository repository,
        CancellationToken cancellationToken)
    {
        await ClearGraphAsync(applicationId, repository.Id, cancellationToken);

        var nodes = new List<GraphNodeEntity>();
        var edges = new List<GraphEdgeEntity>();
        var now = DateTime.UtcNow;

        var repoNode = await _dbContext.SystemGraphNodes
            .FirstOrDefaultAsync(
                n => n.RepositoryId == repository.Id && n.NodeType == GraphNodeType.Repository,
                cancellationToken);

        if (repoNode is null)
        {
            repoNode = CreateNode(applicationId, repository.Id, GraphNodeType.Repository, repository.Name, $"repo:{repository.Id}", now);
            nodes.Add(repoNode);
        }
        else
        {
            nodes.Add(repoNode);
        }

        await AddRepositoryNodesAsync(applicationId, repository, nodes, edges, now, cancellationToken);

        if (_dbContext is DbContext efContext)
        {
            _dbContext.SystemGraphNodes.AddRange(
                nodes.Where(n => efContext.Entry(n).State == EntityState.Detached));
        }
        else
        {
            _dbContext.SystemGraphNodes.AddRange(nodes);
        }

        _dbContext.SystemGraphEdges.AddRange(edges);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Incrementally updated repository graph {RepositoryId}: {NodeCount} nodes, {EdgeCount} edges",
            repository.Id,
            nodes.Count,
            edges.Count);
    }

    private async Task RebuildDatabaseSubgraphAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var dbNodes = await _dbContext.SystemGraphNodes
            .Where(n => n.ApplicationId == applicationId
                        && (n.NodeType == GraphNodeType.Table || n.NodeType == GraphNodeType.Column))
            .ToListAsync(cancellationToken);

        if (dbNodes.Count > 0)
        {
            var dbNodeIds = dbNodes.Select(n => n.Id).ToList();
            var dbEdges = await _dbContext.SystemGraphEdges
                .Where(e => dbNodeIds.Contains(e.SourceNodeId) || dbNodeIds.Contains(e.TargetNodeId))
                .ToListAsync(cancellationToken);

            _dbContext.SystemGraphEdges.RemoveRange(dbEdges);
            _dbContext.SystemGraphNodes.RemoveRange(dbNodes);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var nodes = await _dbContext.SystemGraphNodes
            .Where(n => n.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        var edges = new List<GraphEdgeEntity>();
        var now = DateTime.UtcNow;
        await AddDatabaseNodesAsync(applicationId, nodes, edges, now, cancellationToken);

        if (_dbContext is DbContext efContext)
        {
            _dbContext.SystemGraphNodes.AddRange(
                nodes.Where(n => efContext.Entry(n).State == EntityState.Detached));
        }

        _dbContext.SystemGraphEdges.AddRange(edges);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SystemGraphDto> LoadGraphDtoAsync(
        Guid applicationId,
        Guid? repositoryId,
        CancellationToken cancellationToken)
    {
        var nodes = await _dbContext.SystemGraphNodes
            .Where(n => n.ApplicationId == applicationId
                        && (repositoryId == null || n.RepositoryId == repositoryId || n.RepositoryId == null))
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(n => n.Id).ToList();
        var edges = await _dbContext.SystemGraphEdges
            .Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(cancellationToken);

        return ToDto(applicationId, repositoryId, nodes, edges);
    }

    private async Task SaveSnapshotAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var nodes = await _dbContext.SystemGraphNodes
            .AsNoTracking()
            .Where(n => n.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(n => n.Id).ToList();
        var edges = await _dbContext.SystemGraphEdges
            .AsNoTracking()
            .Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(cancellationToken);

        var refMap = nodes.ToDictionary(n => n.Id, n => n.ReferenceKey);
        var snapshotPayload = new
        {
            Nodes = nodes.Select(n => new
            {
                Id = n.ReferenceKey,
                Label = n.Label,
                Type = n.NodeType.ToString(),
                Detail = n.MetadataJson
            }),
            Edges = edges.Select(e => new
            {
                FromId = refMap.GetValueOrDefault(e.SourceNodeId, e.SourceNodeId.ToString()),
                ToId = refMap.GetValueOrDefault(e.TargetNodeId, e.TargetNodeId.ToString()),
                Label = e.Label ?? e.EdgeType.ToString()
            })
        };

        var existing = await _dbContext.SystemGraphSnapshots
            .Where(s => s.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.SystemGraphSnapshots.RemoveRange(existing);
        }

        var now = DateTime.UtcNow;
        _dbContext.SystemGraphSnapshots.Add(new SystemGraphSnapshot
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            GraphJson = JsonSerializer.Serialize(snapshotPayload),
            BuiltAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
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
                $"service:{repository.Id}:{file.FilePath}",
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
                var tableRef = $"table:{connection.Id}:{table.SchemaName}.{table.TableName}";
                var tableNode = nodes.FirstOrDefault(n => n.ReferenceKey == tableRef)
                    ?? CreateNode(
                        applicationId,
                        null,
                        GraphNodeType.Table,
                        $"{table.SchemaName}.{table.TableName}",
                        tableRef,
                        now);

                if (!nodes.Contains(tableNode))
                {
                    nodes.Add(tableNode);
                }

                foreach (var column in table.Columns)
                {
                    var columnRef = $"column:{table.Id}:{column.Name}";
                    var columnNode = nodes.FirstOrDefault(n => n.ReferenceKey == columnRef)
                        ?? CreateNode(
                            applicationId,
                            null,
                            GraphNodeType.Column,
                            column.Name,
                            columnRef,
                            now);

                    if (!nodes.Contains(columnNode))
                    {
                        nodes.Add(columnNode);
                    }

                    if (!edges.Any(e => e.SourceNodeId == tableNode.Id && e.TargetNodeId == columnNode.Id))
                    {
                        edges.Add(CreateEdge(tableNode, columnNode, GraphEdgeType.Contains, null, 1.0, now));
                    }
                }

                var entityMapping = await _dbContext.CodeEntityMappings
                    .Where(m => m.TableName == table.TableName && m.SchemaName == table.SchemaName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (entityMapping is not null)
                {
                    var entityNode = nodes.FirstOrDefault(n =>
                        n.NodeType == GraphNodeType.Entity
                        && n.Label == entityMapping.EntityClassName);

                    if (entityNode is not null
                        && !edges.Any(e => e.SourceNodeId == entityNode.Id && e.TargetNodeId == tableNode.Id))
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
            if (fromNode is not null && toNode is not null
                && !edges.Any(e => e.SourceNodeId == fromNode.Id && e.TargetNodeId == toNode.Id))
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
