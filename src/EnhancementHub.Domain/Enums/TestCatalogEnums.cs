namespace EnhancementHub.Domain.Enums;

public enum TestCaseStatus
{
    Draft = 0,
    Active = 1,
    Retired = 2
}

public enum TestCaseOrigin
{
    Manual = 0,
    AiGenerated = 1,
    Promoted = 2
}

public enum QaRunnerKind
{
    Simulated = 0,
    Playwright = 1,
    GitHubActions = 2
}
