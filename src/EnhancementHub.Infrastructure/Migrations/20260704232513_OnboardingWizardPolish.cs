using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnboardingWizardPolish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WizardError",
                table: "OnboardingSessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WizardError",
                table: "OnboardingSessions");
        }
    }
}
