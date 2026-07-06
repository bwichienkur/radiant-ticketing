using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Delivery.Commands;
using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EnhancementHub.Tests.Unit;

public sealed class EnhancementRequestStatusTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public EnhancementRequestStatusTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EnhancementHubDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new EnhancementHubDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void CreateValidator_RejectsEmptyTitle()
    {
        var validator = new CreateEnhancementRequestCommandValidator();
        var command = new CreateEnhancementRequestCommand(
            "",
            "Business description",
            "Desired outcome",
            "High",
            null,
            null,
            null,
            null,
            null);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateValidator_RejectsOversizedBusinessDescription()
    {
        var validator = new CreateEnhancementRequestCommandValidator();
        var command = new CreateEnhancementRequestCommand(
            "Valid title",
            new string('x', 8001),
            "Desired outcome",
            "High",
            null,
            null,
            null,
            null,
            null);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BusinessDescription);
    }

    [Theory]
    [InlineData(ApprovalActionType.Approve, EnhancementRequestStatus.PendingApproval, EnhancementRequestStatus.Approved)]
    [InlineData(ApprovalActionType.Reject, EnhancementRequestStatus.PendingApproval, EnhancementRequestStatus.Rejected)]
    [InlineData(ApprovalActionType.RequestClarification, EnhancementRequestStatus.PendingApproval, EnhancementRequestStatus.NeedsClarification)]
    [InlineData(ApprovalActionType.MarkReadyForDevelopment, EnhancementRequestStatus.Approved, EnhancementRequestStatus.ReadyForDevelopment)]
    [InlineData(ApprovalActionType.SendForReanalysis, EnhancementRequestStatus.NeedsClarification, EnhancementRequestStatus.Submitted)]
    public async Task SubmitApprovalAction_UpdatesStatusForActionType(
        ApprovalActionType actionType,
        EnhancementRequestStatus initialStatus,
        EnhancementRequestStatus expectedStatus)
    {
        var approverId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        _dbContext.Users.Add(new User
        {
            Id = approverId,
            Email = "approver@test.local",
            DisplayName = "Approver",
            Role = UserRole.Approver,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _dbContext.EnhancementRequests.Add(new EnhancementRequest
        {
            Id = requestId,
            Title = "Status transition test",
            BusinessDescription = "Test",
            DesiredOutcome = "Test",
            Priority = "Medium",
            SubmittedByUserId = approverId,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(approverId);
        currentUser.SetupGet(x => x.Role).Returns(UserRole.Approver);

        var policyEvaluator = new Mock<IApprovalPolicyEvaluator>();
        policyEvaluator
            .Setup(x => x.EvaluateAsync(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalPolicyEvaluationResult(true, null, null));

        var deliveryHook = new Mock<IDeliveryApprovalHook>();

        var handler = new SubmitApprovalActionCommandHandler(
            _dbContext,
            currentUser.Object,
            new AuditService(_dbContext, currentUser.Object),
            policyEvaluator.Object,
            deliveryHook.Object);

        var result = await handler.Handle(
            new SubmitApprovalActionCommand(requestId, actionType, "Test comment"),
            CancellationToken.None);

        var updated = await _dbContext.EnhancementRequests.FindAsync(requestId);
        updated!.Status.Should().Be(expectedStatus);
        result.PreviousValue.Should().Be(initialStatus.ToString());
        result.NewValue.Should().Be(expectedStatus.ToString());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
