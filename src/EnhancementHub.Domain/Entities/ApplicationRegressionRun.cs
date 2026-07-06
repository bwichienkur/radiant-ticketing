using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class ApplicationRegressionRun : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public string TestUrl { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public QaRunnerKind QaRunner { get; set; }

    public bool IsSimulation { get; set; }

    public int CaseCount { get; set; }

    public int PassedCaseCount { get; set; }

    public string? ReportStoragePath { get; set; }

    public string? ResultsJson { get; set; }

    public Application Application { get; set; } = null!;
}
