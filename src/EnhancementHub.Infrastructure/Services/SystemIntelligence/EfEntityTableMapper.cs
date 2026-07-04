using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class EfEntityTableMapper : IEfEntityTableMapper
{
    private static readonly string[] ExcludedDirectories =
    [
        "bin", "obj", ".git", "node_modules", ".vs", "packages", "TestResults"
    ];

    public async Task<IReadOnlyList<EntityMappingInfo>> MapEntitiesAsync(
        string rootPath,
        IReadOnlyList<string> dbContextTypes,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
        {
            return Array.Empty<EntityMappingInfo>();
        }

        var csharpFiles = Directory
            .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f, rootPath))
            .ToList();

        var mappings = new List<EntityMappingInfo>();
        var fluentMappings = new List<EntityMappingInfo>();

        foreach (var file in csharpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = await File.ReadAllTextAsync(file, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);
            var relativePath = GetRelativePath(rootPath, file);

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var tableAttr = ExtractTableAttribute(classDecl);
                if (tableAttr is not null)
                {
                    mappings.Add(new EntityMappingInfo
                    {
                        EntityClassName = classDecl.Identifier.Text,
                        EntityNamespace = GetNamespace(classDecl),
                        EntityFilePath = relativePath,
                        TableName = tableAttr.Value.TableName,
                        SchemaName = tableAttr.Value.SchemaName,
                        DbContextType = FindOwningDbContext(classDecl, dbContextTypes),
                        MappingSource = EntityMappingSource.Attribute,
                        ConfidenceScore = 0.95
                    });
                }
            }

            foreach (var dbSet in root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                         .Where(p => p.Type is GenericNameSyntax { Identifier.Text: "DbSet" }))
            {
                var entityType = ((GenericNameSyntax)dbSet.Type).TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
                if (string.IsNullOrWhiteSpace(entityType))
                {
                    continue;
                }

                var className = entityType.Split('.').Last();
                if (mappings.Any(m => m.EntityClassName == className && m.MappingSource == EntityMappingSource.Attribute))
                {
                    continue;
                }

                mappings.Add(new EntityMappingInfo
                {
                    EntityClassName = className,
                    EntityNamespace = entityType.Contains('.') ? entityType[..entityType.LastIndexOf('.')] : GetNamespace(dbSet),
                    EntityFilePath = relativePath,
                    TableName = Pluralize(className),
                    SchemaName = "dbo",
                    DbContextType = GetNamespace(dbSet) + "." + GetContainingClassName(dbSet),
                    MappingSource = EntityMappingSource.Fluent,
                    ConfidenceScore = 0.7
                });
            }

            fluentMappings.AddRange(ExtractFluentMappings(root, relativePath, dbContextTypes));
        }

        foreach (var fluent in fluentMappings)
        {
            if (!mappings.Any(m =>
                    m.EntityClassName.Equals(fluent.EntityClassName, StringComparison.Ordinal)
                    && m.TableName.Equals(fluent.TableName, StringComparison.OrdinalIgnoreCase)))
            {
                mappings.Add(fluent);
            }
        }

        return mappings
            .GroupBy(m => $"{m.EntityClassName}|{m.TableName}|{m.SchemaName}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(m => m.ConfidenceScore).First())
            .OrderBy(m => m.EntityClassName)
            .ToList();
    }

    private static IEnumerable<EntityMappingInfo> ExtractFluentMappings(
        SyntaxNode root,
        string filePath,
        IReadOnlyList<string> dbContextTypes)
    {
        var results = new List<EntityMappingInfo>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var expr = invocation.Expression.ToString();
            if (!expr.Contains("ToTable", StringComparison.Ordinal))
            {
                continue;
            }

            var entityType = ExtractGenericEntityType(invocation);
            var tableName = ExtractStringArgument(invocation, 0) ?? entityType;
            var schemaName = ExtractStringArgument(invocation, 1) ?? "dbo";

            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            results.Add(new EntityMappingInfo
            {
                EntityClassName = entityType.Split('.').Last(),
                EntityNamespace = entityType.Contains('.') ? entityType[..entityType.LastIndexOf('.')] : string.Empty,
                EntityFilePath = filePath,
                TableName = tableName,
                SchemaName = schemaName,
                DbContextType = dbContextTypes.FirstOrDefault(),
                MappingSource = EntityMappingSource.Fluent,
                ConfidenceScore = 0.85
            });
        }

        return results;
    }

    private static (string TableName, string SchemaName)? ExtractTableAttribute(ClassDeclarationSyntax classDecl)
    {
        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (!name.EndsWith("Table", StringComparison.Ordinal) && !name.EndsWith("TableAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                var tableName = classDecl.Identifier.Text;
                var schemaName = "dbo";

                if (attr.ArgumentList?.Arguments.Count > 0)
                {
                    var first = attr.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        tableName = first;
                    }
                }

                foreach (var arg in attr.ArgumentList?.Arguments ?? [])
                {
                    if (arg.NameEquals?.Name.Identifier.Text is "Schema")
                    {
                        schemaName = arg.Expression.ToString().Trim('"');
                    }
                }

                return (tableName, schemaName);
            }
        }

        return null;
    }

    private static string? ExtractGenericEntityType(InvocationExpressionSyntax invocation)
    {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        var target = memberAccess?.Expression.ToString() ?? string.Empty;
        if (target.Contains("Entity<", StringComparison.Ordinal))
        {
            var start = target.IndexOf('<');
            var end = target.LastIndexOf('>');
            if (start >= 0 && end > start)
            {
                return target[(start + 1)..end];
            }
        }

        return null;
    }

    private static string? ExtractStringArgument(InvocationExpressionSyntax invocation, int index)
    {
        if (invocation.ArgumentList?.Arguments.Count > index)
        {
            return invocation.ArgumentList.Arguments[index].Expression.ToString().Trim('"');
        }

        return null;
    }

    private static string? FindOwningDbContext(SyntaxNode node, IReadOnlyList<string> dbContextTypes)
    {
        var className = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
        if (className is not null && dbContextTypes.Any(d => d.EndsWith("." + className, StringComparison.Ordinal)))
        {
            return dbContextTypes.First(d => d.EndsWith("." + className, StringComparison.Ordinal));
        }

        return dbContextTypes.FirstOrDefault();
    }

    private static string GetContainingClassName(SyntaxNode node) =>
        node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? string.Empty;

    private static string Pluralize(string name) =>
        name.EndsWith('s') ? name : name + "s";

    private static bool IsExcluded(string filePath, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => ExcludedDirectories.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static string GetRelativePath(string root, string file) =>
        Path.GetRelativePath(root, file).Replace('\\', '/');

    private static string GetNamespace(SyntaxNode node)
    {
        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString() ?? string.Empty;
    }
}
