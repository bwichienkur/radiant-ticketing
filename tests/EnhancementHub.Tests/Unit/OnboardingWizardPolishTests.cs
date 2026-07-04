using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Application.Features.Onboarding.Queries;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class OnboardingWizardPolishTests
{
    [Fact]
    public async Task GetOnboardingWizardPrefill_LoadsApplicationRepositoryAndDatabase()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var teamId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.Teams.Add(new Team
            {
                Id = teamId,
                Name = "Platform Engineering",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.Applications.Add(new Domain.Entities.Application
            {
                Id = applicationId,
                Name = "Orders API",
                BusinessDomain = "Commerce",
                Purpose = "Order processing",
                OwnerTeamId = teamId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.OnboardingSessions.Add(new OnboardingSession
            {
                Id = sessionId,
                ApplicationId = applicationId,
                CurrentStep = OnboardingStep.ConnectDatabase,
                Status = OnboardingSessionStatus.InProgress,
                StartedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.Repositories.Add(new Repository
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Name = "orders-repo",
                Url = "/data/orders",
                DefaultBranch = "develop",
                IndexingStatus = IndexingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.DatabaseConnections.Add(new DatabaseConnection
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Name = "Orders DB",
                Provider = DatabaseProviderType.PostgreSQL,
                ConnectionStringProtected = "protected",
                IsReadOnly = true,
                ScanStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var queryScope = factory.Services.CreateScope();
        var handler = new GetOnboardingWizardPrefillQueryHandler(
            queryScope.ServiceProvider.GetRequiredService<EnhancementHub.Application.Abstractions.IEnhancementHubDbContext>());

        var prefill = await handler.Handle(new GetOnboardingWizardPrefillQuery(sessionId), CancellationToken.None);

        prefill.Step1!.Name.Should().Be("Orders API");
        prefill.Step2!.RepositoryName.Should().Be("orders-repo");
        prefill.Step3!.ConnectionName.Should().Be("Orders DB");
    }

    [Fact]
    public async Task SetOnboardingWizardError_PersistsAndAdvanceClears()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var sessionId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            db.OnboardingSessions.Add(new OnboardingSession
            {
                Id = sessionId,
                CurrentStep = OnboardingStep.ConnectCode,
                Status = OnboardingSessionStatus.InProgress,
                StartedByUserId = Guid.NewGuid(),
                WizardError = "Previous failure",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var commandScope = factory.Services.CreateScope();
        var dbContext = commandScope.ServiceProvider.GetRequiredService<EnhancementHub.Application.Abstractions.IEnhancementHubDbContext>();

        await new SetOnboardingWizardErrorCommandHandler(dbContext)
            .Handle(new SetOnboardingWizardErrorCommand(sessionId, "Repository path is not accessible."), CancellationToken.None);

        var advanced = await new AdvanceOnboardingSessionCommandHandler(dbContext)
            .Handle(new AdvanceOnboardingSessionCommand(sessionId, OnboardingStep.ConnectDatabase), CancellationToken.None);

        advanced.WizardError.Should().BeNull();
    }
}
