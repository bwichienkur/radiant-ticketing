using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase41DeliveryAutomationPhaseA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDeliveryProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeploymentMechanism = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryRepositoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BranchNamingPattern = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CicdPipelineReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CicdProviderOverride = table.Column<int>(type: "INTEGER", nullable: true),
                    SmokeTestPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DatabaseMigrationStrategy = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresHumanProdDeploy = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfigTransformsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionMappingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDeliveryProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDeliveryProfiles_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationDeliveryProfiles_Repositories_PrimaryRepositoryId",
                        column: x => x.PrimaryRepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TenantDeliveryProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultCicdProvider = table.Column<int>(type: "INTEGER", nullable: false),
                    VaultSecretPrefix = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AutoImplementOnApprove = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoDeployToTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequirePullRequestReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireUatSignoff = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireProdChangeWindow = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChangeWindowNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    QaVideoRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDeliveryProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDeliveryProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantDeploymentEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EnvironmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseUrlTemplate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SecretReferencePrefix = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresApprovalForDeploy = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantDeliveryProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDeploymentEnvironments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDeploymentEnvironments_TenantDeliveryProfiles_TenantDeliveryProfileId",
                        column: x => x.TenantDeliveryProfileId,
                        principalTable: "TenantDeliveryProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantDeploymentEnvironments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDeliveryProfiles_ApplicationId",
                table: "ApplicationDeliveryProfiles",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDeliveryProfiles_PrimaryRepositoryId",
                table: "ApplicationDeliveryProfiles",
                column: "PrimaryRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDeliveryProfiles_TenantId",
                table: "TenantDeliveryProfiles",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDeploymentEnvironments_TenantDeliveryProfileId",
                table: "TenantDeploymentEnvironments",
                column: "TenantDeliveryProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDeploymentEnvironments_TenantId_Name",
                table: "TenantDeploymentEnvironments",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDeliveryProfiles");

            migrationBuilder.DropTable(
                name: "TenantDeploymentEnvironments");

            migrationBuilder.DropTable(
                name: "TenantDeliveryProfiles");
        }
    }
}
