using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed class DriftReport
{
    public Guid DatabaseConnectionId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<DriftFindingDto> Findings { get; set; } = Array.Empty<DriftFindingDto>();
    public int TotalFindings => Findings.Count;
    public int CriticalCount => Findings.Count(f => f.Severity == DriftSeverity.Critical);
    public int HighCount => Findings.Count(f => f.Severity == DriftSeverity.High);
}

public sealed class DriftFindingDto
{
    public DriftType DriftType { get; set; }
    public DriftSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CodeReference { get; set; }
    public string? DatabaseReference { get; set; }
    public Guid? RepositoryId { get; set; }
}
