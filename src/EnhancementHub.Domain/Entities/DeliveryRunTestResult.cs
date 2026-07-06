using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DeliveryRunTestResult : BaseEntity
{
    public Guid EnhancementDeliveryRunId { get; set; }

    public Guid TestCaseId { get; set; }

    public Guid TestCaseVersionId { get; set; }

    public string TestCaseTitle { get; set; } = string.Empty;

    public bool IsRegressionCase { get; set; }

    public bool Passed { get; set; }

    public int DurationMs { get; set; }

    public string? Detail { get; set; }

    public string? ScreenshotStoragePath { get; set; }

    public EnhancementDeliveryRun DeliveryRun { get; set; } = null!;

    public TestCase TestCase { get; set; } = null!;

    public TestCaseVersion TestCaseVersion { get; set; } = null!;
}
