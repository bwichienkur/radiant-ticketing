using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase21SystemIntelligencePerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDriftScanAt",
                table: "DatabaseConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentationExportCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MarkdownDocumentation = table.Column<string>(type: "TEXT", nullable: false),
                    MermaidErd = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentationExportCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentationExportCaches_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentationExportCaches_ApplicationId",
                table: "DocumentationExportCaches",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentationExportCaches");

            migrationBuilder.DropColumn(
                name: "LastDriftScanAt",
                table: "DatabaseConnections");
        }
    }
}
