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

    public static readonly Guid DemoRequestId = Guid.Parse("279c38dc-8da4-400b-828f-711726210eb6");
    public static readonly Guid DemoApplicationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DemoTeamId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DemoAdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultTenantId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static async Task SeedAsync(
        IEnhancementHubDbContext db,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            await EnsureDevAdminCredentialsAsync(db, passwordHasher, logger, cancellationToken);
            await SeedDemoPresentationDataAsync(db, logger, cancellationToken);
            return;
        }

        logger.LogInformation("Seeding development admin user.");

        var defaultTenantId = DefaultTenantId;
        if (!await db.Tenants.AnyAsync(cancellationToken))
        {
            db.Tenants.Add(new Tenant
            {
                Id = defaultTenantId,
                Name = "Default Organization",
                Slug = "default",
                Plan = TenantPlan.Enterprise,
                Region = TenantRegion.US,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        var admin = new User
        {
            Id = DemoAdminUserId,
            Email = AdminEmail,
            DisplayName = "System Administrator",
            Department = "IT",
            Role = UserRole.Admin,
            TenantId = defaultTenantId,
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
        await SeedDemoPresentationDataAsync(db, logger, cancellationToken);
    }

    /// <summary>
    /// Ensures rich demo data exists for presentations and screen recordings, even on existing databases.
    /// </summary>
    public static async Task SeedDemoPresentationDataAsync(
        IEnhancementHubDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedDemoSystemIntelligenceDataAsync(db, logger, cancellationToken);

        var now = DateTime.UtcNow;
        var demoRequest = await db.EnhancementRequests
            .AsNoTracking()
            .Include(r => r.Analyses)
            .FirstOrDefaultAsync(r => r.Id == DemoRequestId, cancellationToken);

        if (demoRequest is null)
        {
            logger.LogInformation("Seeding demo enhancement request for presentations.");
            demoRequest = new EnhancementRequest
            {
                Id = DemoRequestId,
                Title = "Add order cancellation reason for compliance",
                BusinessDescription =
                    "Finance and compliance require a structured cancellation reason on every order " +
                    "to support audit trails and revenue recognition reporting.",
                DesiredOutcome =
                    "Approvers can select a reason code when cancelling orders; reports include reason breakdowns.",
                Priority = "High",
                TargetApplicationId = DemoApplicationId,
                SubmittedByUserId = DemoAdminUserId,
                TeamId = DemoTeamId,
                Status = EnhancementRequestStatus.PendingApproval,
                SupportingNotes = "Requested by Finance — Q3 audit readiness.",
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddHours(-1)
            };
            db.EnhancementRequests.Add(demoRequest);
            await db.SaveChangesAsync(cancellationToken);
            demoRequest = await db.EnhancementRequests
                .AsNoTracking()
                .Include(r => r.Analyses)
                .FirstAsync(r => r.Id == DemoRequestId, cancellationToken);
        }
        else
        {
            var statusUpdates = await db.EnhancementRequests
                .Where(r => r.Id == DemoRequestId && r.Status != EnhancementRequestStatus.PendingApproval)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(r => r.Status, EnhancementRequestStatus.PendingApproval)
                        .SetProperty(r => r.UpdatedAt, now),
                    cancellationToken);

            if (statusUpdates > 0)
            {
                logger.LogInformation("Updated demo request status to PendingApproval.");
            }
        }

        if (!demoRequest.Analyses.Any())
        {
            var analysisId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            logger.LogInformation("Seeding demo AI analysis for request {RequestId}.", DemoRequestId);

            var analysis = new EnhancementAnalysis
            {
                Id = analysisId,
                EnhancementRequestId = DemoRequestId,
                FeatureSummary =
                    "Add a required cancellation reason field to the order cancellation workflow with " +
                    "reporting hooks for finance compliance.",
                BusinessRequirement =
                    "Capture structured cancellation reasons for audit and revenue recognition.",
                TechnicalRequirements =
                    "Extend Order entity, API DTOs, and admin UI; add migration for CancellationReasonCode column.",
                ConfidenceScore = 0.91,
                RiskLevel = RiskLevel.Medium,
                RiskExplanation =
                    "Touches order lifecycle and reporting — moderate regression risk in checkout flows.",
                TestingPlan = "Unit tests for validation; integration tests for cancel API; UAT with Finance.",
                RolloutPlan = "Feature flag → pilot tenant → full rollout after approval.",
                RollbackPlan = "Disable feature flag; column nullable so rollback is non-destructive.",
                FeatureCategory = "Compliance",
                BusinessGoal = "Audit readiness for Q3 financial close",
                NeedsClarification = false,
                Version = 1,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddHours(-2)
            };

            db.EnhancementAnalyses.Add(analysis);
            db.AnalysisFindings.AddRange(
                new AnalysisFinding
                {
                    Id = Guid.NewGuid(),
                    EnhancementAnalysisId = analysisId,
                    Category = "Database",
                    Title = "Orders table schema change",
                    Description = "Add CancellationReasonCode (nvarchar(32), NOT NULL with default for legacy rows).",
                    ConfidenceScore = 0.94,
                    IsAiSuggested = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new AnalysisFinding
                {
                    Id = Guid.NewGuid(),
                    EnhancementAnalysisId = analysisId,
                    Category = "API",
                    Title = "Cancel order endpoint",
                    Description = "POST /api/orders/{id}/cancel must accept reasonCode in request body.",
                    ConfidenceScore = 0.88,
                    IsAiSuggested = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            db.AffectedApplications.Add(new AffectedApplication
            {
                Id = Guid.NewGuid(),
                EnhancementAnalysisId = analysisId,
                ApplicationId = DemoApplicationId,
                ImpactDescription = "Order service, admin portal, and nightly compliance export job.",
                ConfidenceScore = 0.92,
                CreatedAt = now,
                UpdatedAt = now
            });

            db.DatabaseChangeRecommendations.Add(new DatabaseChangeRecommendation
            {
                Id = Guid.NewGuid(),
                EnhancementAnalysisId = analysisId,
                TableName = "Orders",
                ChangeType = "AddColumn",
                Description = "CancellationReasonCode varchar(32) NOT NULL DEFAULT 'UNSPECIFIED'",
                MigrationRequired = true,
                ConfidenceScore = 0.93,
                IsAiSuggested = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Comments.AnyAsync(c => c.EnhancementRequestId == DemoRequestId, cancellationToken))
        {
            db.Comments.Add(new Comment
            {
                Id = Guid.NewGuid(),
                EnhancementRequestId = DemoRequestId,
                UserId = DemoAdminUserId,
                Content = "Finance confirmed the reason code list — ready for approver review.",
                IsInternal = true,
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-3)
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.EnhancementRequests.AnyAsync(
                r => r.Id != DemoRequestId && r.Status == EnhancementRequestStatus.Submitted,
                cancellationToken))
        {
            db.EnhancementRequests.Add(new EnhancementRequest
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Title = "Expose shipment tracking webhook for partners",
                BusinessDescription = "Partners need real-time shipment events via webhook.",
                DesiredOutcome = "Documented webhook with HMAC signing and retry policy.",
                Priority = "Medium",
                TargetApplicationId = DemoApplicationId,
                SubmittedByUserId = DemoAdminUserId,
                TeamId = DemoTeamId,
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = now.AddHours(-6),
                UpdatedAt = now.AddHours(-6)
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await SeedDemoSystemGraphAsync(db, logger, cancellationToken);
    }

    private static async Task EnsureDevAdminCredentialsAsync(
        IEnhancementHubDbContext db,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail, cancellationToken);
        if (admin is null)
        {
            return;
        }

        if (!passwordHasher.Verify(AdminPassword, admin.PasswordHash))
        {
            logger.LogWarning("Resetting development admin password to known demo credentials.");
            admin.PasswordHash = passwordHasher.Hash(AdminPassword);
            admin.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDemoSystemGraphAsync(
        IEnhancementHubDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.SystemGraphNodes.AnyAsync(n => n.ApplicationId == DemoApplicationId, cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding demo system graph nodes.");
        var now = DateTime.UtcNow;
        var repositoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var appNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repoNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var ordersTableId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var cancelApiId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        db.SystemGraphNodes.AddRange(
            new SystemGraphNode
            {
                Id = appNodeId,
                ApplicationId = DemoApplicationId,
                NodeType = GraphNodeType.Application,
                Label = "Radiant Commerce Platform",
                ReferenceKey = "app:radiant-commerce",
                LastUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SystemGraphNode
            {
                Id = repoNodeId,
                ApplicationId = DemoApplicationId,
                RepositoryId = repositoryId,
                NodeType = GraphNodeType.Repository,
                Label = "enhancementhub",
                ReferenceKey = "repo:enhancementhub",
                LastUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SystemGraphNode
            {
                Id = ordersTableId,
                ApplicationId = DemoApplicationId,
                NodeType = GraphNodeType.Table,
                Label = "Orders",
                ReferenceKey = "table:Orders",
                MetadataJson = """{"schema":"dbo"}""",
                LastUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SystemGraphNode
            {
                Id = cancelApiId,
                ApplicationId = DemoApplicationId,
                NodeType = GraphNodeType.ApiEndpoint,
                Label = "POST /api/orders/{id}/cancel",
                ReferenceKey = "api:orders.cancel",
                LastUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

        db.SystemGraphEdges.AddRange(
            new SystemGraphEdge
            {
                Id = Guid.NewGuid(),
                SourceNodeId = appNodeId,
                TargetNodeId = repoNodeId,
                EdgeType = GraphEdgeType.Contains,
                Label = "owns",
                ConfidenceScore = 1.0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SystemGraphEdge
            {
                Id = Guid.NewGuid(),
                SourceNodeId = repoNodeId,
                TargetNodeId = ordersTableId,
                EdgeType = GraphEdgeType.MapsTo,
                Label = "maps to",
                ConfidenceScore = 0.95,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SystemGraphEdge
            {
                Id = Guid.NewGuid(),
                SourceNodeId = cancelApiId,
                TargetNodeId = ordersTableId,
                EdgeType = GraphEdgeType.References,
                Label = "updates",
                ConfidenceScore = 0.9,
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync(cancellationToken);
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

        var teamId = DemoTeamId;
        var applicationId = DemoApplicationId;
        var repositoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var now = DateTime.UtcNow;

        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Platform Engineering",
            Description = "Core platform and architecture team",
            TenantId = DefaultTenantId,
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

        var adminId = DemoAdminUserId;
        if (!await db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == adminId, cancellationToken))
        {
            db.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = adminId,
                Role = "Owner",
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
