using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Delivery.Dtos;

public sealed record DeliveryTimelineEventDto(DateTime OccurredAt, string Message);

public sealed record QaTestStepDto(string Step, bool Passed, string Detail);

public sealed record DeliveryRunTestResultDto(
    Guid TestCaseId,
    string Title,
    bool IsRegressionCase,
    bool Passed,
    int DurationMs,
    string? Detail);

public sealed record EnhancementDeliveryRunDto(
    Guid Id,
    Guid EnhancementRequestId,
    int RunNumber,
    DeliveryRunPhase Phase,
    bool IsSimulation,
    string? BranchName,
    string? PullRequestUrl,
    int? PullRequestNumber,
    string? TestUrl,
    string? TestDeployReference,
    IReadOnlyList<QaTestStepDto> QaSteps,
    IReadOnlyList<DeliveryRunTestResultDto> TestCaseResults,
    bool? QaPassed,
    string? QaVideoUrl,
    string? QaReportUrl,
    string QaRunner,
    DateTime? QaStartedAt,
    DateTime? QaFinishedAt,
    bool UatApproved,
    DateTime? UatSignedOffAt,
    string? UatNotes,
    DateTime? ProdScheduledAt,
    string? ProdDeployReference,
    DateTime? ProdDeployedAt,
    IReadOnlyList<DeliveryTimelineEventDto> Timeline,
    string? LastError);
