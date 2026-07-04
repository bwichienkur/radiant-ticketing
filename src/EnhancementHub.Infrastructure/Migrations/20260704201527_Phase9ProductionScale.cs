using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase9ProductionScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeEntityProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodeEntityMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ColumnName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ClrType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsNullable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrimaryKey = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeEntityProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeEntityProperties_CodeEntityMappings_CodeEntityMappingId",
                        column: x => x.CodeEntityMappingId,
                        principalTable: "CodeEntityMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeEntityProperties_CodeEntityMappingId_PropertyName",
                table: "CodeEntityProperties",
                columns: new[] { "CodeEntityMappingId", "PropertyName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeEntityProperties");
        }
    }
}
