using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class TestCaseVersion : BaseEntity
{
    public Guid TestCaseId { get; set; }

    public int Version { get; set; }

    public string Title { get; set; } = string.Empty;

    public string StepsJson { get; set; } = "[]";

    public TestCase TestCase { get; set; } = null!;
}
