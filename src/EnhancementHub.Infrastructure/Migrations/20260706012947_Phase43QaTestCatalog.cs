using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase43QaTestCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QaFinishedAt",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QaRunner",
                table: "EnhancementDeliveryRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "QaStartedAt",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationTestSuites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsDefaultRegression = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTestSuites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationTestSuites_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestSuiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceEnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceEnhancementAnalysisId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RepositoryPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                    TagsJson = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_ApplicationTestSuites_TestSuiteId",
                        column: x => x.TestSuiteId,
                        principalTable: "ApplicationTestSuites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestCases_EnhancementRequests_SourceEnhancementRequestId",
                        column: x => x.SourceEnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TestCaseVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCaseVersions_TestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryRunTestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementDeliveryRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsRegressionCase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ScreenshotStoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRunTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRunTestResults_EnhancementDeliveryRuns_EnhancementDeliveryRunId",
                        column: x => x.EnhancementDeliveryRunId,
                        principalTable: "EnhancementDeliveryRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryRunTestResults_TestCaseVersions_TestCaseVersionId",
                        column: x => x.TestCaseVersionId,
                        principalTable: "TestCaseVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryRunTestResults_TestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTestSuites_ApplicationId_Name",
                table: "ApplicationTestSuites",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunTestResults_EnhancementDeliveryRunId_TestCaseId",
                table: "DeliveryRunTestResults",
                columns: new[] { "EnhancementDeliveryRunId", "TestCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunTestResults_TestCaseId",
                table: "DeliveryRunTestResults",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunTestResults_TestCaseVersionId",
                table: "DeliveryRunTestResults",
                column: "TestCaseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_SourceEnhancementRequestId",
                table: "TestCases",
                column: "SourceEnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_TestSuiteId_Title",
                table: "TestCases",
                columns: new[] { "TestSuiteId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseVersions_TestCaseId_Version",
                table: "TestCaseVersions",
                columns: new[] { "TestCaseId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRunTestResults");

            migrationBuilder.DropTable(
                name: "TestCaseVersions");

            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.DropTable(
                name: "ApplicationTestSuites");

            migrationBuilder.DropColumn(
                name: "QaFinishedAt",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "QaRunner",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "QaStartedAt",
                table: "EnhancementDeliveryRuns");
        }
    }
}
