using System.Data.Common;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Tests.Unit;

public sealed class DocumentationExportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;
    private readonly DocumentationExportService _sut;

    public DocumentationExportServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new EnhancementHubDbContext(
            new DbContextOptionsBuilder<EnhancementHubDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();

        var graphBuilder = new Mock<ISystemGraphBuilder>();
        graphBuilder
            .Setup(g => g.BuildForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemGraphDto
            {
                Nodes =
                [
                    new GraphNodeDto
                    {
                        Id = Guid.NewGuid(),
                        Label = "OrdersController",
                        NodeType = GraphNodeType.ApiEndpoint
                    }
                ],
                Edges = []
            });

        var fingerprintService = new Mock<ISystemIntelligenceFingerprintService>();
        fingerprintService
            .Setup(f => f.ComputeApplicationFingerprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TEST-FINGERPRINT");

        _sut = new DocumentationExportService(
            _dbContext,
            graphBuilder.Object,
            fingerprintService.Object,
            Microsoft.Extensions.Options.Options.Create(new Application.Options.SystemIntelligenceOptions
            {
                DocumentationCacheEnabled = false
            }));
    }

    [Fact]
    public async Task ExportAsync_IncludesMarkdownAndMermaidErd()
    {
        var applicationId = await SeedApplicationWithDatabaseAsync();

        var bundle = await _sut.ExportAsync(applicationId);

        bundle.MarkdownDocumentation.Should().Contain("Sample App");
        bundle.MarkdownDocumentation.Should().Contain("Orders");
        bundle.MermaidErd.Should().Contain("erDiagram");
        bundle.MermaidErd.Should().Contain("Orders");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private async Task<Guid> SeedApplicationWithDatabaseAsync()
    {
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _dbContext.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Platform",
            CreatedAt = now,
            UpdatedAt = now
        });

        _dbContext.Applications.Add(new ApplicationEntity
        {
            Id = appId,
            Name = "Sample App",
            Description = "Test",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now
        });

        var connection = new DatabaseConnection
        {
            Id = connectionId,
            ApplicationId = appId,
            Name = "Primary",
            Provider = DatabaseProviderType.Sqlite,
            ConnectionStringProtected = "protected",
            CreatedAt = now,
            UpdatedAt = now
        };

        connection.Tables.Add(new DatabaseTable
        {
            Id = tableId,
            DatabaseConnectionId = connectionId,
            SchemaName = "dbo",
            TableName = "Orders",
            CapturedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            Columns =
            [
                new DatabaseColumn
                {
                    Id = Guid.NewGuid(),
                    DatabaseTableId = tableId,
                    Name = "Id",
                    DataType = "INTEGER",
                    IsNullable = false,
                    IsPrimaryKey = true,
                    OrdinalPosition = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ]
        });

        _dbContext.DatabaseConnections.Add(connection);
        await _dbContext.SaveChangesAsync();
        return appId;
    }
}
