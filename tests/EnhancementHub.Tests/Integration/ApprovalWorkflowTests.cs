using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class ApprovalWorkflowTests
{
    private readonly TestWebApplicationFactory _factory;

    public ApprovalWorkflowTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Approve_UpdatesStatusAndCreatesAuditLog()
    {
        var builder = _factory.CreateDataBuilder();
        var submitter = await builder.CreateUserAsync(UserRole.Submitter);
        var approver = await builder.CreateUserAsync(UserRole.Approver);
        var request = await builder.CreateEnhancementRequestAsync(
            submitter,
            EnhancementRequestStatus.PendingApproval,
            "Approve me");

        var client = await _factory.CreateAuthenticatedClientAsync(approver);
        var response = await client.PostAsJsonAsync(
            $"/api/approvals/{request.Id}/actions",
            new { actionType = ApprovalActionType.Approve, comments = "Looks good" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();

        var updated = await db.EnhancementRequests.FindAsync(request.Id);
        updated!.Status.Should().Be(EnhancementRequestStatus.Approved);

        var approvalAction = db.ApprovalActions.Single(a => a.EnhancementRequestId == request.Id);
        approvalAction.ActionType.Should().Be(ApprovalActionType.Approve);
        approvalAction.PreviousValue.Should().Be(EnhancementRequestStatus.PendingApproval.ToString());
        approvalAction.NewValue.Should().Be(EnhancementRequestStatus.Approved.ToString());

        var auditLog = db.AuditLogs.Single(a =>
            a.EntityType == nameof(Domain.Entities.EnhancementRequest) &&
            a.EntityId == request.Id &&
            a.Action == ApprovalActionType.Approve.ToString());
        auditLog.Comments.Should().Be("Looks good");
    }
}
