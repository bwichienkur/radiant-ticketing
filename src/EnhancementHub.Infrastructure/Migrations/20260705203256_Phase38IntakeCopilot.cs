using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase38IntakeCopilot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntakeCopilotSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    TurnCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MessagesJson = table.Column<string>(type: "TEXT", nullable: false),
                    DraftJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    SuggestedTemplateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedRequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastAssistantMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeCopilotSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntakeCopilotSessions_EnhancementRequests_CreatedRequestId",
                        column: x => x.CreatedRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IntakeCopilotSessions_EnhancementTemplates_SuggestedTemplateId",
                        column: x => x.SuggestedTemplateId,
                        principalTable: "EnhancementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IntakeCopilotSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeCopilotSessions_CreatedRequestId",
                table: "IntakeCopilotSessions",
                column: "CreatedRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeCopilotSessions_Status",
                table: "IntakeCopilotSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeCopilotSessions_SuggestedTemplateId",
                table: "IntakeCopilotSessions",
                column: "SuggestedTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeCopilotSessions_UserId",
                table: "IntakeCopilotSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeCopilotSessions");
        }
    }
}
