using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;

namespace EnhancementHub.Tests.Unit;

public sealed class BulkApprovalTests
{
    [Fact]
    public async Task BulkSubmitApprovalActions_ReturnsPerItemResults()
    {
        var requestId1 = Guid.NewGuid();
        var requestId2 = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.Is<SubmitApprovalActionCommand>(c => c.EnhancementRequestId == requestId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalActionDto(
                Guid.NewGuid(),
                requestId1,
                null,
                Guid.NewGuid(),
                "Approver",
                ApprovalActionType.Approve,
                null,
                EnhancementRequestStatus.PendingApproval.ToString(),
                EnhancementRequestStatus.Approved.ToString(),
                DateTime.UtcNow));
        mediator
            .Setup(m => m.Send(It.Is<SubmitApprovalActionCommand>(c => c.EnhancementRequestId == requestId2), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Policy blocked"));

        var handler = new BulkSubmitApprovalActionsCommandHandler(mediator.Object);
        var result = await handler.Handle(
            new BulkSubmitApprovalActionsCommand(
                [requestId1, requestId2, requestId1],
                ApprovalActionType.Approve,
                "Batch"),
            CancellationToken.None);

        result.SucceededCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Results.Should().HaveCount(2);
        result.Results.Single(r => r.RequestId == requestId1).Success.Should().BeTrue();
        result.Results.Single(r => r.RequestId == requestId2).Success.Should().BeFalse();
        result.Results.Single(r => r.RequestId == requestId2).ErrorMessage.Should().Contain("Policy blocked");
    }

    [Fact]
    public async Task BulkSubmitApprovalActions_EmptyIds_ReturnsEmptyResult()
    {
        var mediator = new Mock<IMediator>();
        var handler = new BulkSubmitApprovalActionsCommandHandler(mediator.Object);
        var result = await handler.Handle(
            new BulkSubmitApprovalActionsCommand([], ApprovalActionType.Approve, null),
            CancellationToken.None);

        result.SucceededCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Results.Should().BeEmpty();
        mediator.Verify(
            m => m.Send(It.IsAny<SubmitApprovalActionCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
