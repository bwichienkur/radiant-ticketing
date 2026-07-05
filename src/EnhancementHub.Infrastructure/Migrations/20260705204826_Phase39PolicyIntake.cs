using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase39PolicyIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PolicySourceLabel",
                table: "IntakeCopilotSessions",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicySourceText",
                table: "IntakeCopilotSessions",
                type: "TEXT",
                maxLength: 50000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicySourceLabel",
                table: "IntakeCopilotSessions");

            migrationBuilder.DropColumn(
                name: "PolicySourceText",
                table: "IntakeCopilotSessions");
        }
    }
}
