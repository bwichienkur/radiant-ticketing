using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed class EntityMappingInfo
{
    public string EntityClassName { get; set; } = string.Empty;
    public string EntityNamespace { get; set; } = string.Empty;
    public string EntityFilePath { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string? DbContextType { get; set; }
    public EntityMappingSource MappingSource { get; set; }
    public double ConfidenceScore { get; set; }
    public IReadOnlyList<EntityPropertyInfo> Properties { get; set; } = Array.Empty<EntityPropertyInfo>();
}

public sealed class EntityPropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string ClrType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
}

public sealed class RepositoryScanResult
{
    public string RootPath { get; set; } = string.Empty;
    public IReadOnlyList<ScannedNamespace> Namespaces { get; set; } = Array.Empty<ScannedNamespace>();
    public IReadOnlyList<ScannedClass> Classes { get; set; } = Array.Empty<ScannedClass>();
    public IReadOnlyList<ScannedController> Controllers { get; set; } = Array.Empty<ScannedController>();
    public IReadOnlyList<string> DbContextTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<EntityMappingInfo> EntityMappings { get; set; } = Array.Empty<EntityMappingInfo>();
    public int TotalFilesScanned { get; set; }
}

public sealed class ScannedNamespace
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public sealed class ScannedClass
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsStatic { get; set; }
    public bool IsPartial { get; set; }
    public IReadOnlyList<string> Methods { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> BaseTypes { get; set; } = Array.Empty<string>();
}

public sealed class ScannedController
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public IReadOnlyList<string> Actions { get; set; } = Array.Empty<string>();
}
