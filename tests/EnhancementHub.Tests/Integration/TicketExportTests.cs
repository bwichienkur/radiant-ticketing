using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.ExternalTickets.Commands;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EnhancementHub.Tests.Integration;

public sealed class TicketExportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public TicketExportTests()
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
    public async Task Export_UsesMockExporter_AndPersistsTicket()
    {
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        _dbContext.Users.Add(new User
        {
            Id = userId,
            Email = "exporter@test.local",
            DisplayName = "Exporter",
            Role = UserRole.Submitter,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _dbContext.EnhancementRequests.Add(new EnhancementRequest
        {
            Id = requestId,
            Title = "Export to GitHub",
            BusinessDescription = "Need GitHub issue.",
            DesiredOutcome = "Tracked externally.",
            Priority = "Medium",
            SubmittedByUserId = userId,
            Status = EnhancementRequestStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var exporterMock = new Mock<IExternalTicketExporter>();
        exporterMock
            .Setup(x => x.ExportAsync(It.IsAny<ExternalTicketExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalTicketExportResult(
                true,
                "EXT-42",
                "https://tickets.example/EXT-42",
                null));

        var factoryMock = new Mock<IExternalTicketExporterFactory>();
        factoryMock
            .Setup(x => x.GetExporter(ExternalTicketProvider.GitHub))
            .Returns(exporterMock.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(userId);

        var handler = new ExportExternalTicketCommandHandler(
            _dbContext,
            factoryMock.Object,
            currentUser.Object,
            new AuditService(_dbContext, currentUser.Object));

        var result = await handler.Handle(
            new ExportExternalTicketCommand(requestId, ExternalTicketProvider.GitHub),
            CancellationToken.None);

        result.ExternalId.Should().Be("EXT-42");
        result.ExternalUrl.Should().Be("https://tickets.example/EXT-42");

        exporterMock.Verify(
            x => x.ExportAsync(
                It.Is<ExternalTicketExportRequest>(r =>
                    r.EnhancementRequestId == requestId &&
                    r.Title == "Export to GitHub"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var ticket = _dbContext.ExternalTickets.Single(t => t.EnhancementRequestId == requestId);
        ticket.ExternalId.Should().Be("EXT-42");

        var auditLog = _dbContext.AuditLogs.Single(a =>
            a.Action == "ExternalTicketExported" &&
            a.EntityId == ticket.Id);
        auditLog.Comments.Should().Contain("EXT-42");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
