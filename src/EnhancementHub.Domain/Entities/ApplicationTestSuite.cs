using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class ApplicationTestSuite : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public string Name { get; set; } = "Regression";

    public string? Description { get; set; }

    public bool IsDefaultRegression { get; set; } = true;

    public Application Application { get; set; } = null!;

    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
