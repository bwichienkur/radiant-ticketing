namespace EnhancementHub.Application.Abstractions.Models;

public sealed class RefactorBlastRadiusResult
{
    public string TargetName { get; set; } = string.Empty;
    public IReadOnlyList<string> AffectedTables { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AffectedEntities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AffectedServices { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AffectedApiEndpoints { get; set; } = Array.Empty<string>();
    public IReadOnlyList<GraphNodeDto> TraversedNodes { get; set; } = Array.Empty<GraphNodeDto>();
    public int TotalAffectedComponents =>
        AffectedTables.Count + AffectedEntities.Count + AffectedServices.Count + AffectedApiEndpoints.Count;
}
