using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class DocumentationExportService : IDocumentationExportService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ISystemGraphBuilder _graphBuilder;
    private readonly ISystemIntelligenceFingerprintService _fingerprintService;
    private readonly SystemIntelligenceOptions _options;

    public DocumentationExportService(
        IEnhancementHubDbContext dbContext,
        ISystemGraphBuilder graphBuilder,
        ISystemIntelligenceFingerprintService fingerprintService,
        IOptions<SystemIntelligenceOptions> options)
    {
        _dbContext = dbContext;
        _graphBuilder = graphBuilder;
        _fingerprintService = fingerprintService;
        _options = options.Value;
    }

    public async Task<DocumentationBundle> ExportAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        var fingerprint = await _fingerprintService.ComputeApplicationFingerprintAsync(applicationId, cancellationToken);
        var now = DateTime.UtcNow;

        if (_options.DocumentationCacheEnabled)
        {
            var cached = await _dbContext.DocumentationExportCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ApplicationId == applicationId, cancellationToken);

            if (cached is not null
                && cached.ExpiresAt > now
                && cached.SourceFingerprint == fingerprint)
            {
                return new DocumentationBundle
                {
                    ApplicationId = applicationId,
                    ApplicationName = application.Name,
                    MarkdownDocumentation = cached.MarkdownDocumentation,
                    MermaidErd = cached.MermaidErd,
                    GeneratedAt = cached.GeneratedAt
                };
            }
        }

        var graph = await _graphBuilder.BuildForApplicationAsync(applicationId, cancellationToken);

        var connections = await _dbContext.DatabaseConnections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .Include(c => c.Relationships)
            .Where(c => c.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        var markdown = BuildMarkdown(application.Name, connections, graph);
        var mermaid = BuildMermaidErd(connections);
        var bundle = new DocumentationBundle
        {
            ApplicationId = applicationId,
            ApplicationName = application.Name,
            MarkdownDocumentation = markdown,
            MermaidErd = mermaid,
            GeneratedAt = now
        };

        if (_options.DocumentationCacheEnabled)
        {
            await UpsertCacheAsync(applicationId, fingerprint, bundle, cancellationToken);
        }

        return bundle;
    }

    private async Task UpsertCacheAsync(
        Guid applicationId,
        string fingerprint,
        DocumentationBundle bundle,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DocumentationExportCaches
            .FirstOrDefaultAsync(c => c.ApplicationId == applicationId, cancellationToken);

        var expiresAt = bundle.GeneratedAt.AddMinutes(Math.Clamp(_options.DocumentationCacheTtlMinutes, 5, 10_080));
        if (existing is null)
        {
            _dbContext.DocumentationExportCaches.Add(new DocumentationExportCache
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                MarkdownDocumentation = bundle.MarkdownDocumentation,
                MermaidErd = bundle.MermaidErd,
                SourceFingerprint = fingerprint,
                GeneratedAt = bundle.GeneratedAt,
                ExpiresAt = expiresAt,
                CreatedAt = bundle.GeneratedAt,
                UpdatedAt = bundle.GeneratedAt
            });
        }
        else
        {
            existing.MarkdownDocumentation = bundle.MarkdownDocumentation;
            existing.MermaidErd = bundle.MermaidErd;
            existing.SourceFingerprint = fingerprint;
            existing.GeneratedAt = bundle.GeneratedAt;
            existing.ExpiresAt = expiresAt;
            existing.UpdatedAt = bundle.GeneratedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildMarkdown(
        string applicationName,
        IReadOnlyList<DatabaseConnection> connections,
        SystemGraphDto graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {applicationName} — System Documentation");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:u}");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine($"- Graph nodes: {graph.Nodes.Count}");
        sb.AppendLine($"- Graph edges: {graph.Edges.Count}");
        sb.AppendLine($"- Database connections: {connections.Count}");
        sb.AppendLine();

        foreach (var connection in connections)
        {
            sb.AppendLine($"## Database: {connection.Name}");
            sb.AppendLine($"- Provider: {connection.Provider}");
            sb.AppendLine($"- Host: {connection.Host ?? "n/a"}");
            sb.AppendLine($"- Database: {connection.DatabaseName ?? "n/a"}");
            sb.AppendLine($"- Tables: {connection.Tables.Count}");
            sb.AppendLine();

            foreach (var table in connection.Tables.OrderBy(t => t.SchemaName).ThenBy(t => t.TableName))
            {
                sb.AppendLine($"### {table.SchemaName}.{table.TableName}");
                sb.AppendLine("| Column | Type | Nullable | PK | FK |");
                sb.AppendLine("| --- | --- | --- | --- | --- |");
                foreach (var col in table.Columns.OrderBy(c => c.OrdinalPosition))
                {
                    sb.AppendLine($"| {col.Name} | {col.DataType} | {col.IsNullable} | {col.IsPrimaryKey} | {col.IsForeignKey} |");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string BuildMermaidErd(IReadOnlyList<DatabaseConnection> connections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("erDiagram");

        foreach (var connection in connections)
        {
            foreach (var table in connection.Tables)
            {
                var entityName = SanitizeMermaidId($"{table.SchemaName}_{table.TableName}");
                sb.AppendLine($"    {entityName} {{");
                foreach (var col in table.Columns.OrderBy(c => c.OrdinalPosition).Take(20))
                {
                    var type = SanitizeMermaidType(col.DataType);
                    sb.AppendLine($"        {type} {SanitizeMermaidId(col.Name)}");
                }

                sb.AppendLine("    }");
            }
        }

        var relationships = connections.SelectMany(c => c.Relationships).ToList();
        foreach (var rel in relationships)
        {
            var fromTable = connections.SelectMany(c => c.Tables).FirstOrDefault(t => t.Id == rel.FromTableId);
            var toTable = connections.SelectMany(c => c.Tables).FirstOrDefault(t => t.Id == rel.ToTableId);
            if (fromTable is null || toTable is null)
            {
                continue;
            }

            var fromId = SanitizeMermaidId($"{fromTable.SchemaName}_{fromTable.TableName}");
            var toId = SanitizeMermaidId($"{toTable.SchemaName}_{toTable.TableName}");
            sb.AppendLine($"    {fromId} }}o--|| {toId} : \"{rel.FromColumnName}\"");
        }

        return sb.ToString();
    }

    private static string SanitizeMermaidId(string value) =>
        new(value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    private static string SanitizeMermaidType(string dataType) =>
        dataType.Replace(" ", "_", StringComparison.Ordinal);
}
