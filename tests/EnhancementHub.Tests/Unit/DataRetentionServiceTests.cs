using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Options;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Tests.Unit;

public sealed class DataRetentionServiceTests
{
    [Fact]
    public async Task ApplyAsync_DryRun_CountsEligibleRecordsWithoutDeleting()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        await SeedExpiredRecordsAsync(factory);

        using var serviceScope = factory.Services.CreateScope();
        var service = CreateService(serviceScope, aiDays: 365, attachmentDays: 180);

        var preview = await service.ApplyAsync(dryRun: true);
        preview.AiPromptRunsDeleted.Should().Be(1);
        preview.AttachmentsDeleted.Should().Be(1);

        using var verifyScope = factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        (await db.AiPromptRuns.CountAsync()).Should().Be(1);
        (await db.EnhancementAttachments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_DeletesExpiredAiPromptRuns()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        await SeedExpiredRecordsAsync(factory);

        using var serviceScope = factory.Services.CreateScope();
        var service = CreateService(serviceScope, aiDays: 365, attachmentDays: 180);
        var result = await service.ApplyAsync(dryRun: false);

        result.AiPromptRunsDeleted.Should().Be(1);
        result.AttachmentsDeleted.Should().Be(1);

        using var verifyScope = factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        (await db.AiPromptRuns.CountAsync()).Should().Be(0);
        (await db.EnhancementAttachments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AdminRetentionStatusEndpoint_ReturnsStatusForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/retention/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("aiPromptRunsRetentionDays");
    }

    private static async Task SeedExpiredRecordsAsync(TestWebApplicationFactory factory)
    {
        var builder = factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(UserRole.Developer);
        var request = await builder.CreateEnhancementRequestAsync(user);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();

        var oldRequest = await db.EnhancementRequests.SingleAsync(r => r.Id == request.Id);
        oldRequest.CreatedAt = DateTime.UtcNow.AddDays(-400);
        oldRequest.UpdatedAt = DateTime.UtcNow.AddDays(-400);

        db.AiPromptRuns.Add(new AiPromptRun
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.Id,
            WorkflowStep = "analysis",
            PromptVersion = "v1",
            ModelName = "gpt-4o-mini",
            SystemPrompt = "system",
            UserPrompt = "user",
            Status = AiRunStatus.Completed,
            StartedAt = DateTime.UtcNow.AddDays(-400),
            CreatedAt = DateTime.UtcNow.AddDays(-400),
            UpdatedAt = DateTime.UtcNow.AddDays(-400)
        });

        db.EnhancementAttachments.Add(new EnhancementAttachment
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.Id,
            FileName = "old.pdf",
            ContentType = "application/pdf",
            StoragePath = "attachments/old.pdf",
            UploadedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-200),
            UpdatedAt = DateTime.UtcNow.AddDays(-200)
        });

        await db.SaveChangesAsync();
    }

    private static DataRetentionService CreateService(
        IServiceScope scope,
        int aiDays,
        int attachmentDays)
    {
        return new DataRetentionService(
            scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEnhancementHubDbContext>(),
            scope.ServiceProvider.GetRequiredService<Application.Abstractions.IFileStorageService>(),
            Options.Create(new RetentionOptions
            {
                Enabled = true,
                AiPromptRunsDays = aiDays,
                AttachmentsDays = attachmentDays,
                BatchSize = 500
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DataRetentionService>.Instance);
    }
}
