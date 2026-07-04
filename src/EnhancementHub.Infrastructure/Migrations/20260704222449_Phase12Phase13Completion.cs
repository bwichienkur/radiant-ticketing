using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase12Phase13Completion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscoveryJobState",
                table: "OnboardingSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OnPremAgentId",
                table: "OnboardingSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OnPremConnectionId",
                table: "OnboardingSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanDetails",
                table: "EnhancementAttachments",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScanStatus",
                table: "EnhancementAttachments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscoveryJobState",
                table: "OnboardingSessions");

            migrationBuilder.DropColumn(
                name: "OnPremAgentId",
                table: "OnboardingSessions");

            migrationBuilder.DropColumn(
                name: "OnPremConnectionId",
                table: "OnboardingSessions");

            migrationBuilder.DropColumn(
                name: "ScanDetails",
                table: "EnhancementAttachments");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "EnhancementAttachments");
        }
    }
}
