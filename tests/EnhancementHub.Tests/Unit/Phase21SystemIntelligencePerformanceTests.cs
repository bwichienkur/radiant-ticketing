using EnhancementHub.Application.Common;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase21SystemIntelligencePerformanceTests
{
    [Fact]
    public void SystemGraphQueryHelper_LimitsDepthFromRoot()
    {
        var nodes = new List<SystemGraphNodeDto>
        {
            new("app:1", "App", "Application", null),
            new("repo:1", "Repo", "Repository", null),
            new("entity:1", "Order", "Entity", null),
            new("table:1", "dbo.Orders", "Table", null)
        };

        var edges = new List<SystemGraphEdgeDto>
        {
            new("app:1", "repo:1", "Contains"),
            new("repo:1", "entity:1", "Contains"),
            new("entity:1", "table:1", "MapsTo")
        };

        var result = SystemGraphQueryHelper.Apply(nodes, edges, "app:1", maxDepth: 1, page: 1, pageSize: 50);

        result.Nodes.Should().HaveCount(2);
        result.Nodes.Select(n => n.Id).Should().BeEquivalentTo(["app:1", "repo:1"]);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public void SystemGraphQueryHelper_PaginatesFilteredNodes()
    {
        var nodes = Enumerable.Range(1, 5)
            .Select(i => new SystemGraphNodeDto($"node:{i}", $"Node {i}", "Service", null))
            .ToList();

        var edges = nodes.Zip(nodes.Skip(1), (a, b) => new SystemGraphEdgeDto(a.Id, b.Id, "next")).ToList();

        var result = SystemGraphQueryHelper.Apply(nodes, edges, rootNodeId: "node:1", maxDepth: 10, page: 2, pageSize: 2);

        result.Nodes.Should().HaveCount(2);
        result.TotalNodeCount.Should().Be(5);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public async Task DetectDriftIfStaleAsync_SkipsWhenSourceUnchanged()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = new EnhancementHubDbContext(
            new DbContextOptionsBuilder<EnhancementHubDbContext>().UseSqlite(connection).Options);
        await dbContext.Database.EnsureCreatedAsync();

        var connectionId = await SeedDriftScenarioAsync(dbContext);
        var dbConnection = await dbContext.DatabaseConnections.FirstAsync(c => c.Id == connectionId);
        dbConnection.LastDriftScanAt = DateTime.UtcNow;
        dbConnection.LastScannedAt = DateTime.UtcNow.AddMinutes(-5);
        await dbContext.SaveChangesAsync();

        var notifications = new Mock<EnhancementHub.Application.Abstractions.INotificationPublisher>();
        var sut = new SchemaDriftDetectorService(
            dbContext,
            notifications.Object,
            new SystemIntelligenceFingerprintService(dbContext),
            Microsoft.Extensions.Options.Options.Create(new SystemIntelligenceOptions { DiffOnlyDriftEnabled = true }),
            NullLogger<SchemaDriftDetectorService>.Instance);

        var first = await sut.DetectDriftAsync(connectionId);
        first.Findings.Should().NotBeEmpty();

        dbConnection = await dbContext.DatabaseConnections.FirstAsync(c => c.Id == connectionId);
        dbConnection.LastDriftScanAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        notifications.Invocations.Clear();
        var skipped = await sut.DetectDriftIfStaleAsync(connectionId);

        skipped.Findings.Should().NotBeEmpty();
        notifications.Verify(
            n => n.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DocumentationExport_UsesCacheWhenFingerprintMatches()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = new EnhancementHubDbContext(
            new DbContextOptionsBuilder<EnhancementHubDbContext>().UseSqlite(connection).Options);
        await dbContext.Database.EnsureCreatedAsync();

        var applicationId = await SeedDocumentationAppAsync(dbContext);
        var graphBuilder = new Mock<EnhancementHub.Application.Abstractions.ISystemGraphBuilder>();
        graphBuilder
            .Setup(g => g.BuildForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancementHub.Application.Abstractions.Models.SystemGraphDto
            {
                ApplicationId = applicationId,
                Nodes = [],
                Edges = []
            });

        var fingerprintService = new SystemIntelligenceFingerprintService(dbContext);
        var options = Microsoft.Extensions.Options.Options.Create(new SystemIntelligenceOptions
        {
            DocumentationCacheEnabled = true,
            DocumentationCacheTtlMinutes = 60
        });

        var sut = new DocumentationExportService(dbContext, graphBuilder.Object, fingerprintService, options);

        _ = await sut.ExportAsync(applicationId);
        _ = await sut.ExportAsync(applicationId);

        graphBuilder.Verify(
            g => g.BuildForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SystemMapPagedEndpoint_ReturnsPaginationMetadata()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        Guid appId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
            var teamId = Guid.NewGuid();
            appId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.Teams.Add(new Team { Id = teamId, Name = "Platform", CreatedAt = now, UpdatedAt = now });
            db.Applications.Add(new ApplicationEntity
            {
                Id = appId,
                Name = "Paged Map App",
                OwnerTeamId = teamId,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = admin.Id,
                Role = "Owner",
                CreatedAt = now,
                UpdatedAt = now
            });

            for (var i = 0; i < 3; i++)
            {
                db.SystemGraphNodes.Add(new SystemGraphNode
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = appId,
                    NodeType = GraphNodeType.Service,
                    Label = $"Service{i}",
                    ReferenceKey = $"svc:{i}",
                    LastUpdatedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync(
            $"/api/system-map/{appId}/paged?page=1&pageSize=2&depth=4");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("totalNodeCount");
        json.Should().Contain("truncated");
    }

    private static async Task<Guid> SeedDriftScenarioAsync(EnhancementHubDbContext dbContext)
    {
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var repoId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedAt = now, UpdatedAt = now });
        dbContext.Applications.Add(new ApplicationEntity
        {
            Id = appId,
            Name = "App",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.Repositories.Add(new Repository
        {
            Id = repoId,
            ApplicationId = appId,
            Name = "Repo",
            Url = "https://example.com/repo.git",
            Provider = ExternalTicketProvider.GitHub,
            LastIndexedAt = now.AddMinutes(-10),
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.DatabaseConnections.Add(new DatabaseConnection
        {
            Id = connectionId,
            ApplicationId = appId,
            Name = "Primary",
            Provider = DatabaseProviderType.Sqlite,
            ConnectionStringProtected = "protected",
            ScanStatus = "Completed",
            LastScannedAt = now.AddMinutes(-10),
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.CodeEntityMappings.Add(new CodeEntityMapping
        {
            Id = Guid.NewGuid(),
            RepositoryId = repoId,
            EntityClassName = "Order",
            EntityNamespace = "App",
            EntityFilePath = "Order.cs",
            TableName = "Orders",
            SchemaName = "dbo",
            MappingSource = EntityMappingSource.Attribute,
            ConfidenceScore = 0.9,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync();
        return connectionId;
    }

    private static async Task<Guid> SeedDocumentationAppAsync(EnhancementHubDbContext dbContext)
    {
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.Teams.Add(new Team { Id = teamId, Name = "Team", CreatedAt = now, UpdatedAt = now });
        dbContext.Applications.Add(new ApplicationEntity
        {
            Id = appId,
            Name = "Docs App",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.DatabaseConnections.Add(new DatabaseConnection
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            Name = "Primary",
            Provider = DatabaseProviderType.Sqlite,
            ConnectionStringProtected = "protected",
            CreatedAt = now,
            UpdatedAt = now,
            Tables =
            [
                new DatabaseTable
                {
                    Id = Guid.NewGuid(),
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
                            Name = "Id",
                            DataType = "INTEGER",
                            IsNullable = false,
                            IsPrimaryKey = true,
                            OrdinalPosition = 1,
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    ]
                }
            ]
        });

        await dbContext.SaveChangesAsync();
        return appId;
    }
}
