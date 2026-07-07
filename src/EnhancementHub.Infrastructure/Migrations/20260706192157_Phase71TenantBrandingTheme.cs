using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase71TenantBrandingTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThemePreference",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TenantBrandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AccentColor = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantBrandings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrandings_TenantId",
                table: "TenantBrandings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBrandings");

            migrationBuilder.DropColumn(
                name: "ThemePreference",
                table: "Users");
        }
    }
}
