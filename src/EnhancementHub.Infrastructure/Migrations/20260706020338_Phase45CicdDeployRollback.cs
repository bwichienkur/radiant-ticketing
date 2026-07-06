using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase45CicdDeployRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowOneClickProdDeploy",
                table: "TenantDeliveryProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowOneClickRollback",
                table: "TenantDeliveryProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowProdToTestRefresh",
                table: "TenantDeliveryProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TestDataStrategy",
                table: "TenantDeliveryProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PostDeploySmokePassed",
                table: "EnhancementDeliveryRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProdArtifactReference",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RollbackTargetCommitSha",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RollbackTargetDeployReference",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RolledBackAt",
                table: "EnhancementDeliveryRuns",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowOneClickProdDeploy",
                table: "TenantDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "AllowOneClickRollback",
                table: "TenantDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "AllowProdToTestRefresh",
                table: "TenantDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "TestDataStrategy",
                table: "TenantDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "PostDeploySmokePassed",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "ProdArtifactReference",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "RollbackTargetCommitSha",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "RollbackTargetDeployReference",
                table: "EnhancementDeliveryRuns");

            migrationBuilder.DropColumn(
                name: "RolledBackAt",
                table: "EnhancementDeliveryRuns");
        }
    }
}
