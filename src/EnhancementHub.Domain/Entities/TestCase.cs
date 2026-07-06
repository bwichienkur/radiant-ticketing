using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class TestCase : BaseEntity
{
    public Guid TestSuiteId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;

    public TestCaseOrigin Origin { get; set; } = TestCaseOrigin.Manual;

    public Guid? SourceEnhancementRequestId { get; set; }

    public Guid? SourceEnhancementAnalysisId { get; set; }

    public string? RepositoryPath { get; set; }

    public string StepsJson { get; set; } = "[]";

    public string? TagsJson { get; set; }

    public int SortOrder { get; set; }

    public int CurrentVersion { get; set; } = 1;

    public ApplicationTestSuite TestSuite { get; set; } = null!;

    public EnhancementRequest? SourceEnhancementRequest { get; set; }

    public ICollection<TestCaseVersion> Versions { get; set; } = new List<TestCaseVersion>();
}
