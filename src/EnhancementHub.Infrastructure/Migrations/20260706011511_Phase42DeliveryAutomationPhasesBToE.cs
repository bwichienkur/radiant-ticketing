using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase42DeliveryAutomationPhasesBToE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnhancementDeliveryRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PullRequestUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    CommitSha = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TestUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TestDeployReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    QaStepsJson = table.Column<string>(type: "TEXT", nullable: true),
                    QaPassed = table.Column<bool>(type: "INTEGER", nullable: true),
                    QaVideoStoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    QaReportStoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UatSignedOffByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UatSignedOffAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UatNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    UatApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProdScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProdDeployReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProdDeployedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimelineJson = table.Column<string>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancementDeliveryRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancementDeliveryRuns_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementDeliveryRuns_EnhancementRequestId_RunNumber",
                table: "EnhancementDeliveryRuns",
                columns: new[] { "EnhancementRequestId", "RunNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnhancementDeliveryRuns");
        }
    }
}
