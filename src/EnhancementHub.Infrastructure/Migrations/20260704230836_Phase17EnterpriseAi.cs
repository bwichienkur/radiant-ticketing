using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase17EnterpriseAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiPromptRuns_EnhancementRequests_EnhancementRequestId",
                table: "AiPromptRuns");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnhancementRequestId",
                table: "AiPromptRuns",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "AiPromptRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "AiPromptRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCostUsd",
                table: "AiPromptRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "AiPromptRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "AiPromptRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowStep",
                table: "AiPromptRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AiPromptRuns_EnhancementRequests_EnhancementRequestId",
                table: "AiPromptRuns",
                column: "EnhancementRequestId",
                principalTable: "EnhancementRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiPromptRuns_EnhancementRequests_EnhancementRequestId",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "AiPromptRuns");

            migrationBuilder.DropColumn(
                name: "WorkflowStep",
                table: "AiPromptRuns");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnhancementRequestId",
                table: "AiPromptRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiPromptRuns_EnhancementRequests_EnhancementRequestId",
                table: "AiPromptRuns",
                column: "EnhancementRequestId",
                principalTable: "EnhancementRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
