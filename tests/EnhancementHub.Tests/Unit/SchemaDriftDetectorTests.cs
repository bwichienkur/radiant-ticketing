using System.Data.Common;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Tests.Unit;

public sealed class SchemaDriftDetectorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;
    private readonly SchemaDriftDetectorService _sut;

    public SchemaDriftDetectorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new EnhancementHubDbContext(
            new DbContextOptionsBuilder<EnhancementHubDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();
        _sut = new SchemaDriftDetectorService(_dbContext, NullLogger<SchemaDriftDetectorService>.Instance);
    }

    [Fact]
    public async Task DetectDriftAsync_FlagsMissingTableInDatabase()
    {
        var (connectionId, _) = await SeedConnectionWithMappingAsync("Orders", "dbo");

        var report = await _sut.DetectDriftAsync(connectionId);

        report.Findings.Should().Contain(f =>
            f.DriftType == DriftType.MissingInDatabase
            && f.Title.Contains("Orders", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DetectDriftAsync_FlagsOrphanTableInDatabase()
    {
        var (connectionId, _) = await SeedConnectionWithTableAsync("LegacyAudit", "dbo");

        var report = await _sut.DetectDriftAsync(connectionId);

        report.Findings.Should().Contain(f =>
            f.DriftType == DriftType.OrphanTable
            && f.Title.Contains("LegacyAudit", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private async Task<(Guid ConnectionId, Guid RepositoryId)> SeedConnectionWithMappingAsync(
        string tableName,
        string schema)
    {
        var app = CreateApplication();
        var repo = CreateRepository(app.Id);
        var connection = CreateConnection(app.Id);

        _dbContext.Applications.Add(app);
        _dbContext.Repositories.Add(repo);
        _dbContext.DatabaseConnections.Add(connection);
        _dbContext.CodeEntityMappings.Add(new CodeEntityMapping
        {
            Id = Guid.NewGuid(),
            RepositoryId = repo.Id,
            EntityClassName = tableName.TrimEnd('s'),
            EntityNamespace = "SampleApp.Data",
            EntityFilePath = $"Data/{tableName.TrimEnd('s')}.cs",
            TableName = tableName,
            SchemaName = schema,
            MappingSource = EntityMappingSource.Attribute,
            ConfidenceScore = 0.95,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        return (connection.Id, repo.Id);
    }

    private async Task<(Guid ConnectionId, Guid RepositoryId)> SeedConnectionWithTableAsync(
        string tableName,
        string schema)
    {
        var app = CreateApplication();
        var repo = CreateRepository(app.Id);
        var connection = CreateConnection(app.Id);
        connection.Tables.Add(new DatabaseTable
        {
            Id = Guid.NewGuid(),
            DatabaseConnectionId = connection.Id,
            SchemaName = schema,
            TableName = tableName,
            CapturedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _dbContext.Applications.Add(app);
        _dbContext.Repositories.Add(repo);
        _dbContext.DatabaseConnections.Add(connection);
        await _dbContext.SaveChangesAsync();
        return (connection.Id, repo.Id);
    }

    private static ApplicationEntity CreateApplication()
    {
        var teamId = Guid.NewGuid();
        return new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Sample App",
            Description = "Test",
            OwnerTeamId = teamId,
            OwnerTeam = new Team
            {
                Id = teamId,
                Name = "Platform",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Repository CreateRepository(Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        Name = "sample-repo",
        Url = "/tmp/sample",
        Provider = ExternalTicketProvider.GitHub,
        DefaultBranch = "main",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static DatabaseConnection CreateConnection(Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        Name = "Primary",
        Provider = DatabaseProviderType.Sqlite,
        ConnectionStringProtected = "protected",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
