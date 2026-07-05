using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase24ProductDifferentiation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Applications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApprovalPolicyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumRiskLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApplicationTier = table.Column<int>(type: "INTEGER", nullable: true),
                    RequiredRole = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalPolicyRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnhancementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DomainCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BusinessDescription = table.Column<string>(type: "TEXT", nullable: false),
                    DesiredOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SupportingNotes = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancementTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicyRules_Priority",
                table: "ApprovalPolicyRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancementTemplates_DomainCategory",
                table: "EnhancementTemplates",
                column: "DomainCategory");

            var seedTime = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "EnhancementTemplates",
                columns: new[]
                {
                    "Id", "Name", "DomainCategory", "Title", "BusinessDescription", "DesiredOutcome",
                    "Priority", "SupportingNotes", "IsActive", "CreatedAt", "UpdatedAt"
                },
                values: new object[,]
                {
                    {
                        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001"),
                        "Security hardening review",
                        "Security",
                        "Security control enhancement",
                        "We need to strengthen authentication and authorization controls for a regulated workload.",
                        "Documented security improvements with approval-ready change package and test plan.",
                        "High",
                        "Include threat model and compliance mapping.",
                        true,
                        seedTime,
                        seedTime
                    },
                    {
                        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002"),
                        "Performance optimization",
                        "Performance",
                        "Performance improvement initiative",
                        "Users report slow response times under peak load for a critical workflow.",
                        "Measurable latency reduction with rollout and rollback plans.",
                        "Medium",
                        "Capture baseline metrics before implementation.",
                        true,
                        seedTime,
                        seedTime
                    },
                    {
                        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003"),
                        "Compliance remediation",
                        "Compliance",
                        "Compliance gap remediation",
                        "An audit identified gaps in data retention and access logging requirements.",
                        "Remediation plan aligned to policy with evidence for auditors.",
                        "High",
                        "Reference SOC 2 control IDs where applicable.",
                        true,
                        seedTime,
                        seedTime
                    }
                });

            migrationBuilder.InsertData(
                table: "ApprovalPolicyRules",
                columns: new[]
                {
                    "Id", "Name", "IsEnabled", "Priority", "MinimumRiskLevel", "Department",
                    "ApplicationTier", "RequiredRole", "BlockApproval", "Message", "CreatedAt", "UpdatedAt"
                },
                values: new object[,]
                {
                    {
                        new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001"),
                        "Critical risk requires Admin approval",
                        true,
                        1,
                        3,
                        null,
                        null,
                        0,
                        true,
                        "Critical-risk enhancements require Admin approval.",
                        seedTime,
                        seedTime
                    },
                    {
                        new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0002"),
                        "Critical-tier application requires Admin",
                        true,
                        2,
                        null,
                        null,
                        1,
                        0,
                        true,
                        "Enhancements targeting critical-tier applications require Admin approval.",
                        seedTime,
                        seedTime
                    },
                    {
                        new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0003"),
                        "Finance department requires Admin",
                        true,
                        3,
                        null,
                        "Finance",
                        null,
                        0,
                        true,
                        "Finance department enhancements require Admin approval.",
                        seedTime,
                        seedTime
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalPolicyRules");

            migrationBuilder.DropTable(
                name: "EnhancementTemplates");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Applications");
        }
    }
}
