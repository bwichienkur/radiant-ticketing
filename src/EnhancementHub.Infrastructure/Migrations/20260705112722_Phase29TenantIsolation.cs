using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase29TenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabaseSchemaName",
                table: "Tenants",
                type: "TEXT",
                maxLength: 63,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsolationMode",
                table: "Tenants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SchemaProvisionedAt",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DatabaseSchemaName",
                table: "Tenants",
                column: "DatabaseSchemaName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_DatabaseSchemaName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DatabaseSchemaName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsolationMode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SchemaProvisionedAt",
                table: "Tenants");
        }
    }
}
