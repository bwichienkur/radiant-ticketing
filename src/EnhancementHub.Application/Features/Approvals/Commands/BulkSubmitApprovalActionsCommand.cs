using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;

namespace EnhancementHub.Application.Features.Approvals.Commands;

public sealed record BulkSubmitApprovalActionsCommand(
    IReadOnlyList<Guid> RequestIds,
    ApprovalActionType ActionType,
    string? Comments) : IRequest<BulkApprovalActionResultDto>;

public sealed record BulkApprovalItemResult(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    ApprovalActionDto? Action);

public sealed record BulkApprovalActionResultDto(
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<BulkApprovalItemResult> Results);

public sealed class BulkSubmitApprovalActionsCommandHandler
    : IRequestHandler<BulkSubmitApprovalActionsCommand, BulkApprovalActionResultDto>
{
    private readonly IMediator _mediator;

    public BulkSubmitApprovalActionsCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<BulkApprovalActionResultDto> Handle(
        BulkSubmitApprovalActionsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RequestIds is not { Count: > 0 })
        {
            return new BulkApprovalActionResultDto(0, 0, []);
        }

        var results = new List<BulkApprovalItemResult>();
        foreach (var requestId in request.RequestIds.Distinct())
        {
            try
            {
                var action = await _mediator.Send(
                    new SubmitApprovalActionCommand(requestId, request.ActionType, request.Comments),
                    cancellationToken);
                results.Add(new BulkApprovalItemResult(requestId, true, null, action));
            }
            catch (Exception ex)
            {
                results.Add(new BulkApprovalItemResult(requestId, false, ex.Message, null));
            }
        }

        var succeeded = results.Count(r => r.Success);
        return new BulkApprovalActionResultDto(succeeded, results.Count - succeeded, results);
    }
}
