namespace EnhancementHub.Application.Features.Delivery.Dtos;

public sealed record TestCaseSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string Origin,
    Guid? SourceEnhancementRequestId,
    int CurrentVersion);

public sealed record ApplicationTestSuiteDto(
    Guid Id,
    Guid ApplicationId,
    string Name,
    IReadOnlyList<TestCaseSummaryDto> TestCases);

public sealed record ApplicationRegressionRunDto(
    Guid Id,
    Guid ApplicationId,
    string TestUrl,
    bool Passed,
    string QaRunner,
    bool IsSimulation,
    int CaseCount,
    int PassedCaseCount,
    DateTime CreatedAt);
