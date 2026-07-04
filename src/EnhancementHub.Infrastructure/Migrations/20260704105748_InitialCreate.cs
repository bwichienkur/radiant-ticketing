using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiPromptConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SystemPromptTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    OutputSchema = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BusinessDomain = table.Column<string>(type: "TEXT", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerTeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RiskSensitiveAreas = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Teams_OwnerTeamId",
                        column: x => x.OwnerTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PreviousValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AiModelUsed = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RetrievedContextReferences = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnhancementRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BusinessDescription = table.Column<string>(type: "TEXT", nullable: false),
                    DesiredOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetApplicationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestedDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SupportingNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancementRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancementRequests_Applications_TargetApplicationId",
                        column: x => x.TargetApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EnhancementRequests_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EnhancementRequests_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultBranch = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GitTokenSecretName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastIndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IndexingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repositories_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    IsInternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnhancementAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeatureSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    BusinessRequirement = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TechnicalRequirements = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskExplanation = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TestingPlan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RolloutPlan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RollbackPlan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OpenQuestions = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ApprovalChecklist = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FeatureCategory = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BusinessGoal = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NeedsClarification = table.Column<bool>(type: "INTEGER", nullable: false),
                    AmbiguityNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsApprovedSnapshot = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancementAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancementAnalyses_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnhancementAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancementAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancementAttachments_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnhancementAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ExternalUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalTickets_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalTickets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BusinessDomain = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    KeyComponents = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DatabaseUsage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ExternalIntegrations = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    InternalDependencies = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DeploymentNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RiskSensitiveAreas = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OwnershipMetadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationProfiles_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationProfiles_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastCommitHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastIndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositoryBranches_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffectedApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImpactDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectedApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffectedApplications_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffectedApplications_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffectedRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImpactDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectedRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffectedRepositories_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffectedRepositories_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiPromptRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PromptVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", nullable: true),
                    SystemPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    UserPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    RawResponse = table.Column<string>(type: "TEXT", nullable: true),
                    StructuredResponse = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiPromptRuns_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiPromptRuns_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHumanApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisFindings_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiChangeRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    HttpMethod = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiChangeRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiChangeRecommendations_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", nullable: true),
                    PreviousValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    AiModelUsed = table.Column<string>(type: "TEXT", nullable: true),
                    PromptVersion = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalActions_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalActions_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalActions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseChangeRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MigrationRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    IsAiSuggested = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseChangeRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseChangeRecommendations_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityConcerns = table.Column<string>(type: "TEXT", nullable: true),
                    PerformanceConcerns = table.Column<string>(type: "TEXT", nullable: true),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskAssessments_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndexedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Project = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ComponentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Namespace = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ExtractedDependencies = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedDatabaseObjects = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedApis = table.Column<string>(type: "TEXT", nullable: true),
                    CommitHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    LastIndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmbeddingVector = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexedFiles_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndexedFiles_RepositoryBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "RepositoryBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffectedComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IndexedFileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ComponentPath = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentType = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpactDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectedComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffectedComponents_EnhancementAnalyses_EnhancementAnalysisId",
                        column: x => x.EnhancementAnalysisId,
                        principalTable: "EnhancementAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffectedComponents_IndexedFiles_IndexedFileId",
                        column: x => x.IndexedFileId,
                        principalTable: "IndexedFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IndexedSymbols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IndexedFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SymbolName = table.Column<string>(type: "TEXT", nullable: false),
                    SymbolKind = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    LineStart = table.Column<int>(type: "INTEGER", nullable: false),
                    LineEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexedSymbols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexedSymbols_IndexedFiles_IndexedFileId",
                        column: x => x.IndexedFileId,
                        principalTable: "IndexedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetrievedContextItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AiPromptRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IndexedFileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    SourceReference = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    RelevanceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetrievedContextItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetrievedContextItems_AiPromptRuns_AiPromptRunId",
                        column: x => x.AiPromptRunId,
                        principalTable: "AiPromptRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetrievedContextItems_IndexedFiles_IndexedFileId",
                        column: x => x.IndexedFileId,
                        principalTable: "IndexedFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffectedApplications_ApplicationId",
                table: "AffectedApplications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectedApplications_EnhancementAnalysisId",
                table: "AffectedApplications",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectedComponents_EnhancementAnalysisId",
                table: "AffectedComponents",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectedComponents_IndexedFileId",
                table: "AffectedComponents",
                column: "IndexedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectedRepositories_EnhancementAnalysisId",
                table: "AffectedRepositories",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectedRepositories_RepositoryId",
                table: "AffectedRepositories",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptConfigurations_Name_Version",
                table: "AiPromptConfigurations",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptRuns_EnhancementAnalysisId",
                table: "AiPromptRuns",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptRuns_EnhancementRequestId",
                table: "AiPromptRuns",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisFindings_EnhancementAnalysisId",
                table: "AnalysisFindings",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiChangeRecommendations_EnhancementAnalysisId",
                table: "ApiChangeRecommendations",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationProfiles_ApplicationId_RepositoryId",
                table: "ApplicationProfiles",
                columns: new[] { "ApplicationId", "RepositoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationProfiles_RepositoryId",
                table: "ApplicationProfiles",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OwnerTeamId",
                table: "Applications",
                column: "OwnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalActions_EnhancementAnalysisId",
                table: "ApprovalActions",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalActions_EnhancementRequestId",
                table: "ApprovalActions",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalActions_UserId",
                table: "ApprovalActions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_EnhancementRequestId",
                table: "Comments",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseChangeRecommendations_EnhancementAnalysisId",
                table: "DatabaseChangeRecommendations",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementAnalyses_EnhancementRequestId_Version",
                table: "EnhancementAnalyses",
                columns: new[] { "EnhancementRequestId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementAttachments_EnhancementRequestId",
                table: "EnhancementAttachments",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementAttachments_UploadedByUserId",
                table: "EnhancementAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementRequests_Status",
                table: "EnhancementRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementRequests_SubmittedByUserId",
                table: "EnhancementRequests",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementRequests_TargetApplicationId",
                table: "EnhancementRequests",
                column: "TargetApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementRequests_TeamId",
                table: "EnhancementRequests",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTickets_CreatedByUserId",
                table: "ExternalTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTickets_EnhancementRequestId",
                table: "ExternalTickets",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTickets_Provider_ExternalId",
                table: "ExternalTickets",
                columns: new[] { "Provider", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexedFiles_BranchId",
                table: "IndexedFiles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_IndexedFiles_RepositoryId_BranchId_FilePath",
                table: "IndexedFiles",
                columns: new[] { "RepositoryId", "BranchId", "FilePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndexedSymbols_IndexedFileId",
                table: "IndexedSymbols",
                column: "IndexedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_ApplicationId",
                table: "Repositories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryBranches_RepositoryId_BranchName",
                table: "RepositoryBranches",
                columns: new[] { "RepositoryId", "BranchName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetrievedContextItems_AiPromptRunId",
                table: "RetrievedContextItems",
                column: "AiPromptRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RetrievedContextItems_IndexedFileId",
                table: "RetrievedContextItems",
                column: "IndexedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_EnhancementAnalysisId",
                table: "RiskAssessments",
                column: "EnhancementAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectedApplications");

            migrationBuilder.DropTable(
                name: "AffectedComponents");

            migrationBuilder.DropTable(
                name: "AffectedRepositories");

            migrationBuilder.DropTable(
                name: "AiPromptConfigurations");

            migrationBuilder.DropTable(
                name: "AnalysisFindings");

            migrationBuilder.DropTable(
                name: "ApiChangeRecommendations");

            migrationBuilder.DropTable(
                name: "ApplicationProfiles");

            migrationBuilder.DropTable(
                name: "ApprovalActions");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "DatabaseChangeRecommendations");

            migrationBuilder.DropTable(
                name: "EnhancementAttachments");

            migrationBuilder.DropTable(
                name: "ExternalTickets");

            migrationBuilder.DropTable(
                name: "IndexedSymbols");

            migrationBuilder.DropTable(
                name: "RetrievedContextItems");

            migrationBuilder.DropTable(
                name: "RiskAssessments");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "AiPromptRuns");

            migrationBuilder.DropTable(
                name: "IndexedFiles");

            migrationBuilder.DropTable(
                name: "EnhancementAnalyses");

            migrationBuilder.DropTable(
                name: "RepositoryBranches");

            migrationBuilder.DropTable(
                name: "EnhancementRequests");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
