using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Onboarding;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Tests.Unit;

public sealed class OnboardingWizardTests
{
    [Fact]
    public void OnboardingSessionDto_MapsSessionFields()
    {
        var session = new OnboardingSession
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Application = new Domain.Entities.Application { Name = "Orders API" },
            CurrentStep = OnboardingStep.ConnectCode,
            Status = OnboardingSessionStatus.InProgress,
            SkipDatabase = false,
            DiscoveryStatus = "Indexing...",
            CreatedAt = DateTime.UtcNow
        };

        var dto = OnboardingSessionMapper.ToDto(session);

        dto.ApplicationName.Should().Be("Orders API");
        dto.CurrentStep.Should().Be(OnboardingStep.ConnectCode);
        dto.Status.Should().Be(OnboardingSessionStatus.InProgress);
    }

    [Fact]
    public async Task CreateApplication_CreatesDefaultTeamWhenMissing()
    {
        var options = new DbContextOptionsBuilder<EnhancementHubDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var context = new EnhancementHubDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var db = context;
        IEnhancementHubDbContext dbContext = context;
        var handler = new CreateApplicationCommandHandler(dbContext, new NoOpAuditService());

        var result = await handler.Handle(
            new CreateApplicationCommand("Billing Service", "Finance", "Invoicing", null, "PII", null),
            CancellationToken.None);

        result.Name.Should().Be("Billing Service");
        (await db.Teams.CountAsync()).Should().Be(1);
        (await db.Applications.CountAsync()).Should().Be(1);
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task LogAsync(
            string action,
            string entityType,
            Guid? entityId,
            string details,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
