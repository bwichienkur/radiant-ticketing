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
