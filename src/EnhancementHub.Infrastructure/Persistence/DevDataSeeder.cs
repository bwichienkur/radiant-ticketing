using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    }
}
