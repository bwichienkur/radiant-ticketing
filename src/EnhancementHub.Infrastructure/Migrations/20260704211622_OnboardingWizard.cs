using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnboardingWizard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentStep = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkipDatabase = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiscoveryStatus = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DiscoveryCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingSessions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_ApplicationId",
                table: "OnboardingSessions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_StartedByUserId",
                table: "OnboardingSessions",
                column: "StartedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_Status",
                table: "OnboardingSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingSessions");
        }
    }
}
