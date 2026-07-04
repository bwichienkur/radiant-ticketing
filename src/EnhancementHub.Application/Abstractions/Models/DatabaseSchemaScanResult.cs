namespace EnhancementHub.Application.Abstractions.Models;

public sealed class DatabaseSchemaScanResult
{
    public string? DatabaseName { get; set; }
    public string? Host { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<ScannedTable> Tables { get; set; } = Array.Empty<ScannedTable>();
    public IReadOnlyList<ScannedRelationship> Relationships { get; set; } = Array.Empty<ScannedRelationship>();
}

public sealed class ScannedTable
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public long? RowCountEstimate { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<ScannedColumn> Columns { get; set; } = Array.Empty<ScannedColumn>();
}

public sealed class ScannedColumn
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public int OrdinalPosition { get; set; }
}

public sealed class ScannedRelationship
{
    public string FromSchema { get; set; } = "dbo";
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToSchema { get; set; } = "dbo";
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = "OneToMany";
}
