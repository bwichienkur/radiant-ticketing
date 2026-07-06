using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Common;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.SystemIntelligence.Queries;

public sealed record GetSystemMapQuery(Guid ApplicationId) : IRequest<SystemMapDto>;

public sealed class GetSystemMapQueryHandler : IRequestHandler<GetSystemMapQuery, SystemMapDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GetSystemMapQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<SystemMapDto> Handle(GetSystemMapQuery request, CancellationToken cancellationToken)
    {
        var application = await _accessService.GetAccessibleApplicationAsync(
            request.ApplicationId,
            cancellationToken);

        var snapshot = await _dbContext.SystemGraphSnapshots
            .AsNoTracking()
            .Where(s => s.ApplicationId == request.ApplicationId)
            .OrderByDescending(s => s.BuiltAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is not null)
        {
            var graph = JsonSerializer.Deserialize<SnapshotGraph>(snapshot.GraphJson);
            if (graph is not null)
            {
                return Map(application.Name, request.ApplicationId, graph, snapshot.BuiltAt);
            }
        }

        var nodes = await _dbContext.SystemGraphNodes
            .AsNoTracking()
            .Where(n => n.ApplicationId == request.ApplicationId)
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(n => n.Id).ToList();
        var edges = await _dbContext.SystemGraphEdges
            .AsNoTracking()
            .Where(e => nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(cancellationToken);

        var refMap = nodes.ToDictionary(n => n.Id, n => n.ReferenceKey);

        return new SystemMapDto(
            request.ApplicationId,
            application.Name,
            nodes.Select(n => new SystemGraphNodeDto(n.ReferenceKey, n.Label, n.NodeType.ToString(), n.MetadataJson)).ToList(),
            edges.Select(e => new SystemGraphEdgeDto(
                refMap.GetValueOrDefault(e.SourceNodeId, e.SourceNodeId.ToString()),
                refMap.GetValueOrDefault(e.TargetNodeId, e.TargetNodeId.ToString()),
                e.Label ?? e.EdgeType.ToString())).ToList(),
            nodes.MaxBy(n => n.LastUpdatedAt)?.LastUpdatedAt);
    }

    private static SystemMapDto Map(string? appName, Guid appId, SnapshotGraph graph, DateTime builtAt) =>
        new(
            appId,
            appName,
            graph.Nodes.Select(n => new SystemGraphNodeDto(n.Id, n.Label, n.Type, n.Detail)).ToList(),
            graph.Edges.Select(e => new SystemGraphEdgeDto(e.FromId, e.ToId, e.Label)).ToList(),
            builtAt);

    private sealed record SnapshotGraph(
        IReadOnlyList<SnapshotNode> Nodes,
        IReadOnlyList<SnapshotEdge> Edges);

    private sealed record SnapshotNode(string Id, string Label, string Type, string? Detail);

    private sealed record SnapshotEdge(string FromId, string ToId, string Label);
}

public sealed record GetSystemMapPagedQuery(
    Guid ApplicationId,
    int? MaxDepth = null,
    int Page = 1,
    int? PageSize = null,
    string? RootNodeId = null) : IRequest<SystemMapPagedDto>;

public sealed class GetSystemMapPagedQueryHandler : IRequestHandler<GetSystemMapPagedQuery, SystemMapPagedDto>
{
    private readonly IMediator _mediator;
    private readonly Microsoft.Extensions.Options.IOptions<Application.Options.SystemIntelligenceOptions> _options;

    public GetSystemMapPagedQueryHandler(
        IMediator mediator,
        Microsoft.Extensions.Options.IOptions<Application.Options.SystemIntelligenceOptions> options)
    {
        _mediator = mediator;
        _options = options;
    }

    public async Task<SystemMapPagedDto> Handle(GetSystemMapPagedQuery request, CancellationToken cancellationToken)
    {
        var map = await _mediator.Send(new GetSystemMapQuery(request.ApplicationId), cancellationToken);
        var pageSize = Math.Clamp(
            request.PageSize ?? _options.Value.GraphQueryDefaultPageSize,
            1,
            _options.Value.GraphQueryMaxPageSize);
        var maxDepth = Math.Clamp(
            request.MaxDepth ?? _options.Value.GraphQueryDefaultMaxDepth,
            1,
            20);

        var filtered = Application.Common.SystemGraphQueryHelper.Apply(
            map.Nodes,
            map.Edges,
            request.RootNodeId,
            maxDepth,
            request.Page,
            pageSize);

        return new SystemMapPagedDto(
            map.ApplicationId,
            map.ApplicationName,
            filtered.Nodes,
            filtered.Edges,
            map.BuiltAt,
            filtered.TotalNodeCount,
            request.Page,
            pageSize,
            maxDepth,
            filtered.Truncated);
    }
}

public sealed record GetDatabaseSchemaQuery(Guid ConnectionId) : IRequest<DatabaseSchemaDto>;

public sealed class GetDatabaseSchemaQueryHandler : IRequestHandler<GetDatabaseSchemaQuery, DatabaseSchemaDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GetDatabaseSchemaQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<DatabaseSchemaDto> Handle(GetDatabaseSchemaQuery request, CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleConnectionAsync(request.ConnectionId, cancellationToken);

        var connection = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.DatabaseConnection), request.ConnectionId);

        var tables = await _dbContext.DatabaseTables
            .AsNoTracking()
            .Include(t => t.Columns)
            .Where(t => t.DatabaseConnectionId == request.ConnectionId)
            .OrderBy(t => t.TableName)
            .ToListAsync(cancellationToken);

        var relationships = await _dbContext.DatabaseRelationships
            .AsNoTracking()
            .Include(r => r.FromTable)
            .Include(r => r.ToTable)
            .Where(r => r.DatabaseConnectionId == request.ConnectionId)
            .ToListAsync(cancellationToken);

        return new DatabaseSchemaDto(
            connection.Id,
            connection.Name,
            tables.Select(t => new DatabaseTableDto(
                t.Id,
                t.SchemaName,
                t.TableName,
                t.Columns.OrderBy(c => c.OrdinalPosition).Select(c => new DatabaseColumnDto(
                    c.Name,
                    c.DataType,
                    c.IsNullable,
                    c.IsPrimaryKey,
                    c.IsForeignKey,
                    c.OrdinalPosition)).ToList())).ToList(),
            relationships.Select(r => new DatabaseRelationshipDto(
                r.FromTable.TableName,
                r.FromColumnName,
                r.ToTable.TableName,
                r.ToColumnName,
                r.RelationshipType)).ToList());
    }
}

public sealed record ListDatabaseConnectionsQuery(Guid? ApplicationId = null)
    : IRequest<IReadOnlyList<DatabaseConnectionDto>>;

public sealed class ListDatabaseConnectionsQueryHandler
    : IRequestHandler<ListDatabaseConnectionsQuery, IReadOnlyList<DatabaseConnectionDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public ListDatabaseConnectionsQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<DatabaseConnectionDto>> Handle(
        ListDatabaseConnectionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ApplicationId.HasValue)
        {
            await _accessService.EnsureAccessibleApplicationAsync(
                request.ApplicationId.Value,
                cancellationToken);
        }

        var accessibleApplicationIds = _accessService
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(a => a.Id);

        var query = _dbContext.DatabaseConnections
            .AsNoTracking()
            .Include(c => c.Application)
            .Where(c => accessibleApplicationIds.Contains(c.ApplicationId))
            .AsQueryable();

        if (request.ApplicationId.HasValue)
        {
            query = query.Where(c => c.ApplicationId == request.ApplicationId.Value);
        }

        var items = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

        return items.Select(c => new DatabaseConnectionDto(
            c.Id,
            c.ApplicationId,
            c.Application?.Name,
            c.Name,
            c.Provider,
            c.IsReadOnly,
            c.ScanStatus,
            c.LastScannedAt,
            c.ScanError)).ToList();
    }
}

public sealed record GetErdDiagramQuery(Guid ApplicationId) : IRequest<ErdDiagramDto>;

public sealed class GetErdDiagramQueryHandler : IRequestHandler<GetErdDiagramQuery, ErdDiagramDto>
{
    private readonly IMediator _mediator;

    public GetErdDiagramQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<ErdDiagramDto> Handle(GetErdDiagramQuery request, CancellationToken cancellationToken)
    {
        var map = await _mediator.Send(new GetSystemMapQuery(request.ApplicationId), cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("erDiagram");

        foreach (var node in map.Nodes.Where(n => n.Type == "Table"))
        {
            sb.AppendLine($"    {Sanitize(node.Label)} {{");
            sb.AppendLine("        string id");
            sb.AppendLine("    }");
        }

        foreach (var edge in map.Edges)
        {
            var from = Sanitize(map.Nodes.FirstOrDefault(n => n.Id == edge.FromId)?.Label ?? edge.FromId);
            var to = Sanitize(map.Nodes.FirstOrDefault(n => n.Id == edge.ToId)?.Label ?? edge.ToId);
            sb.AppendLine($"    {from} ||--o{{ {to} : \"{edge.Label}\"");
        }

        return new ErdDiagramDto(request.ApplicationId, sb.ToString());
    }

    private static string Sanitize(string value) =>
        new(value.Where(char.IsLetterOrDigit).ToArray());
}

public sealed record GetDriftRequestDraftQuery(Guid FindingId) : IRequest<DriftRequestDraftDto>;

public sealed class GetDriftRequestDraftQueryHandler
    : IRequestHandler<GetDriftRequestDraftQuery, DriftRequestDraftDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GetDriftRequestDraftQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<DriftRequestDraftDto> Handle(
        GetDriftRequestDraftQuery request,
        CancellationToken cancellationToken)
    {
        var finding = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Include(f => f.DatabaseConnection)
            .ThenInclude(c => c.Application)
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.SchemaDriftFinding), request.FindingId);

        await _accessService.EnsureAccessibleConnectionAsync(
            finding.DatabaseConnectionId,
            cancellationToken);

        if (finding.IsResolved)
        {
            throw new InvalidOperationException("Cannot create a request from a resolved drift finding.");
        }

        var connection = finding.DatabaseConnection;
        var applicationId = connection.ApplicationId;
        var priority = MapDriftSeverityToPriority(finding.Severity);
        var businessDescription = string.IsNullOrWhiteSpace(finding.Description)
            ? $"Schema drift detected: {finding.Title}"
            : finding.Description;
        var desiredOutcome = $"Align live database schema with indexed code expectations for {connection.Name}.";

        if (!string.IsNullOrWhiteSpace(finding.CodeReference))
        {
            businessDescription = $"{businessDescription}\n\nCode reference: {finding.CodeReference}";
        }

        if (!string.IsNullOrWhiteSpace(finding.DatabaseReference))
        {
            businessDescription = $"{businessDescription}\n\nDatabase reference: {finding.DatabaseReference}";
        }

        return new DriftRequestDraftDto(
            finding.Id,
            $"Remediate schema drift: {finding.Title}",
            businessDescription,
            desiredOutcome,
            priority,
            applicationId,
            DriftRequestProvenance.BuildSupportingNotes(finding.Id, connection.Name, finding.Severity),
            connection.Id,
            connection.Name,
            finding.Severity.ToString());
    }

    private static string MapDriftSeverityToPriority(Domain.Enums.DriftSeverity severity) =>
        severity switch
        {
            Domain.Enums.DriftSeverity.Critical => "Critical",
            Domain.Enums.DriftSeverity.High => "High",
            Domain.Enums.DriftSeverity.Medium => "Medium",
            _ => "Low"
        };
}

public sealed record GetDriftReportQuery(Guid ConnectionId, Guid? RepositoryId = null)
    : IRequest<DriftReportDto>;

public sealed class GetDriftReportQueryHandler : IRequestHandler<GetDriftReportQuery, DriftReportDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GetDriftReportQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<DriftReportDto> Handle(GetDriftReportQuery request, CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleConnectionAsync(request.ConnectionId, cancellationToken);

        var findingsQuery = _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Where(f => f.DatabaseConnectionId == request.ConnectionId);

        if (request.RepositoryId.HasValue)
        {
            findingsQuery = findingsQuery.Where(f => f.RepositoryId == request.RepositoryId);
        }

        var findings = await findingsQuery
            .OrderByDescending(f => f.DetectedAt)
            .ThenByDescending(f => f.Severity)
            .ToListAsync(cancellationToken);

        var report = await _dbContext.SchemaDriftReports
            .AsNoTracking()
            .Where(r => r.ConnectionId == request.ConnectionId && r.RepositoryId == request.RepositoryId)
            .OrderByDescending(r => r.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new DriftReportDto(
            request.ConnectionId,
            request.RepositoryId,
            report?.DetectedAt ?? findings.FirstOrDefault()?.DetectedAt,
            findings.Select(f => new SchemaDriftFindingDto(
                f.Id,
                f.DriftType,
                f.Severity,
                f.Title,
                f.Description,
                f.CodeReference,
                f.DatabaseReference,
                f.DetectedAt,
                f.IsResolved)).ToList());
    }
}

public sealed record GetRefactorPlanQuery(Guid PlanId) : IRequest<RefactorPlanDetailDto>;

public sealed class GetRefactorPlanQueryHandler : IRequestHandler<GetRefactorPlanQuery, RefactorPlanDetailDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GetRefactorPlanQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<RefactorPlanDetailDto> Handle(GetRefactorPlanQuery request, CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleRefactorPlanAsync(request.PlanId, cancellationToken);

        var plan = await _dbContext.RefactorPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.RefactorPlan), request.PlanId);

        BlastRadiusResultDto? blastRadius = null;
        if (!string.IsNullOrWhiteSpace(plan.BlastRadiusJson))
        {
            var model = JsonSerializer.Deserialize<Abstractions.Models.RefactorBlastRadiusResult>(plan.BlastRadiusJson);
            if (model is not null)
            {
                blastRadius = BlastRadiusMapper.ToDto(model);
            }
        }

        return new RefactorPlanDetailDto(
            plan.Id,
            plan.Title,
            plan.TargetDescription,
            plan.MigrationStepsJson,
            blastRadius,
            plan.Status,
            plan.CreatedAt);
    }
}

public sealed record ListRefactorPlansQuery(Guid? ApplicationId = null, Guid? ConnectionId = null)
    : IRequest<IReadOnlyList<RefactorPlanDto>>;

public sealed class ListRefactorPlansQueryHandler
    : IRequestHandler<ListRefactorPlansQuery, IReadOnlyList<RefactorPlanDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public ListRefactorPlansQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<RefactorPlanDto>> Handle(
        ListRefactorPlansQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ApplicationId.HasValue)
        {
            await _accessService.EnsureAccessibleApplicationAsync(
                request.ApplicationId.Value,
                cancellationToken);
        }

        if (request.ConnectionId.HasValue)
        {
            await _accessService.EnsureAccessibleConnectionAsync(
                request.ConnectionId.Value,
                cancellationToken);
        }

        var accessibleApplicationIds = _accessService
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(a => a.Id);

        var query = _dbContext.RefactorPlans
            .AsNoTracking()
            .Where(p =>
                (p.DatabaseConnectionId.HasValue
                    && _dbContext.DatabaseConnections.Any(c =>
                        c.Id == p.DatabaseConnectionId
                        && accessibleApplicationIds.Contains(c.ApplicationId)))
                || (p.RepositoryId.HasValue
                    && _dbContext.Repositories.Any(r =>
                        r.Id == p.RepositoryId
                        && accessibleApplicationIds.Contains(r.ApplicationId))));

        if (request.ConnectionId.HasValue)
        {
            query = query.Where(p => p.DatabaseConnectionId == request.ConnectionId);
        }
        else if (request.ApplicationId.HasValue)
        {
            var connectionIds = await _dbContext.DatabaseConnections
                .Where(c => c.ApplicationId == request.ApplicationId.Value)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(p => p.DatabaseConnectionId.HasValue && connectionIds.Contains(p.DatabaseConnectionId.Value));
        }

        var plans = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

        return plans.Select(p => new RefactorPlanDto(
            p.Id,
            p.Title,
            p.TargetDescription,
            p.Status,
            p.RiskLevel,
            p.ConfidenceScore,
            p.DatabaseConnectionId,
            p.RepositoryId,
            p.CreatedAt)).ToList();
    }
}
