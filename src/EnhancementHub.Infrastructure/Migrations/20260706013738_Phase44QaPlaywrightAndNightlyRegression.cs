using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase44QaPlaywrightAndNightlyRegression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationRegressionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false),
                    QaRunner = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false),
                    CaseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PassedCaseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportStoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ResultsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRegressionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRegressionRuns_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRegressionRuns_ApplicationId_CreatedAt",
                table: "ApplicationRegressionRuns",
                columns: new[] { "ApplicationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRegressionRuns");
        }
    }
}
