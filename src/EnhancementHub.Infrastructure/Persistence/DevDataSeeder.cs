using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Infrastructure.Persistence;

public static class DevDataSeeder
{
    public const string AdminEmail = "admin@enhancementhub.dev";
    public const string AdminPassword = "password123";

    public static async Task SeedAsync(
        IEnhancementHubDbContext db,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding development admin user.");

        var admin = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = AdminEmail,
            DisplayName = "System Administrator",
            Department = "IT",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordHash = passwordHasher.Hash(AdminPassword),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);

        if (!await db.AiPromptConfigurations.AnyAsync(cancellationToken))
        {
            db.AiPromptConfigurations.Add(new AiPromptConfiguration
            {
                Id = Guid.NewGuid(),
                Name = "EnhancementAnalysis",
                Version = "v1",
                SystemPromptTemplate = "You are an enterprise software analyst.",
                UserPromptTemplate = "Analyze the following enhancement request: {{title}}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await db.SystemSettings.AnyAsync(cancellationToken))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = "AutoAnalysisEnabled",
                Value = "true",
                Category = "AI",
                Description = "Automatically analyze submitted enhancement requests.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development seed data created.");

        await SeedDemoSystemIntelligenceDataAsync(db, logger, cancellationToken);
    }

    private static async Task SeedDemoSystemIntelligenceDataAsync(
        IEnhancementHubDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Applications.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding demo System Intelligence data.");

        var teamId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var applicationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var repositoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var now = DateTime.UtcNow;

        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Platform Engineering",
            Description = "Core platform and architecture team",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Applications.Add(new ApplicationEntity
        {
            Id = applicationId,
            Name = "Radiant Commerce Platform",
            BusinessDomain = "E-Commerce",
            Purpose = "Order management and fulfillment",
            Description = "Demo application for architecture intelligence",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Repositories.Add(new Repository
        {
            Id = repositoryId,
            ApplicationId = applicationId,
            Name = "enhancementhub",
            Url = "/workspace",
            Provider = ExternalTicketProvider.GitHub,
            DefaultBranch = "main",
            IndexingStatus = IndexingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.DatabaseConnections.Add(new DatabaseConnection
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ApplicationId = applicationId,
            Name = "EnhancementHub SQLite",
            Provider = DatabaseProviderType.Sqlite,
            ConnectionStringProtected = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("Data Source=enhancementhub.db")),
            DatabaseName = "enhancementhub",
            IsReadOnly = true,
            ScanStatus = nameof(SchemaScanStatus.Pending),
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo System Intelligence data created.");
    }
}
