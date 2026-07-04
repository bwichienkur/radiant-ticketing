using EnhancementHub.Application.AuditLogs;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class AuditLogExportTests
{
    [Fact]
    public async Task ExportAuditLogs_ReturnsCsvWithHeadersAndFilteredRows()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        var builder = factory.CreateDataBuilder();
        var user = await builder.CreateUserAsync(Domain.Enums.UserRole.Admin);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            var now = DateTime.UtcNow;

            db.AuditLogs.AddRange(
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Approved",
                    EntityType = "EnhancementRequest",
                    EntityId = Guid.NewGuid(),
                    UserId = user.Id,
                    Comments = "Approved for development",
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Indexed",
                    EntityType = "Repository",
                    EntityId = Guid.NewGuid(),
                    UserId = user.Id,
                    Comments = "Repository indexed",
                    CreatedAt = now.AddDays(-30),
                    UpdatedAt = now.AddDays(-30)
                });

            await db.SaveChangesAsync();
        }

        using var queryScope = factory.Services.CreateScope();
        var handler = new ExportAuditLogsQueryHandler(
            queryScope.ServiceProvider.GetRequiredService<EnhancementHub.Application.Abstractions.IEnhancementHubDbContext>());

        var result = await handler.Handle(
            new ExportAuditLogsQuery(
                "csv",
                EntityType: "EnhancementRequest",
                From: DateTime.UtcNow.AddDays(-7)),
            CancellationToken.None);

        var csv = System.Text.Encoding.UTF8.GetString(result.Content);
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");
        csv.Should().Contain("Approved");
        csv.Should().NotContain("Repository indexed");
    }

    [Fact]
    public async Task ExportAuditLogs_ReturnsJsonArray()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var handler = new ExportAuditLogsQueryHandler(
            scope.ServiceProvider.GetRequiredService<EnhancementHub.Application.Abstractions.IEnhancementHubDbContext>());

        var result = await handler.Handle(new ExportAuditLogsQuery("json"), CancellationToken.None);
        var json = System.Text.Encoding.UTF8.GetString(result.Content);

        result.ContentType.Should().Be("application/json");
        json.TrimStart().Should().StartWith("[");
    }

    [Fact]
    public async Task AuditLogExportEndpoint_RequiresAdminRole()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var developer = await builder.CreateUserAsync(Domain.Enums.UserRole.Developer);

        using var client = await factory.CreateAuthenticatedClientAsync(developer);
        var response = await client.GetAsync("/api/auditlogs/export?format=csv");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuditLogExportEndpoint_ReturnsCsvForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var builder = factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/auditlogs/export?format=csv");

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }
}
