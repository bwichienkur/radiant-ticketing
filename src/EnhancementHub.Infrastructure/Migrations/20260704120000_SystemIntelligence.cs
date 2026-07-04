using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnhancementHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SystemIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionStringProtected = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DatabaseName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastScannedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScanStatus = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ScanError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseConnections_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeEntityMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityClassName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    EntityNamespace = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    EntityFilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SchemaName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DbContextType = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    MappingSource = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeEntityMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeEntityMappings_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeTableReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    SourceMember = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SchemaName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeTableReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeTableReferences_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemGraphNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NodeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ReferenceKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemGraphNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemGraphNodes_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SystemGraphNodes_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SchemaName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    RowCountEstimate = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseTables_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefactorPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnhancementRequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DatabaseConnectionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    TargetDescription = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    BlastRadiusJson = table.Column<string>(type: "TEXT", nullable: true),
                    MigrationStepsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedByAi = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefactorPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefactorPlans_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RefactorPlans_EnhancementRequests_EnhancementRequestId",
                        column: x => x.EnhancementRequestId,
                        principalTable: "EnhancementRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RefactorPlans_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SchemaDriftFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DriftType = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CodeReference = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DatabaseReference = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaDriftFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaDriftFindings_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchemaDriftFindings_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SystemGraphEdges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EdgeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemGraphEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemGraphEdges_SystemGraphNodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "SystemGraphNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemGraphEdges_SystemGraphNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "SystemGraphNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DatabaseTableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    MaxLength = table.Column<int>(type: "INTEGER", nullable: true),
                    IsNullable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrimaryKey = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForeignKey = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrdinalPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseColumns_DatabaseTables_DatabaseTableId",
                        column: x => x.DatabaseTableId,
                        principalTable: "DatabaseTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromTableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromColumnName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ToTableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToColumnName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseRelationships_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatabaseRelationships_DatabaseTables_FromTableId",
                        column: x => x.FromTableId,
                        principalTable: "DatabaseTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatabaseRelationships_DatabaseTables_ToTableId",
                        column: x => x.ToTableId,
                        principalTable: "DatabaseTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeEntityMappings_RepositoryId_EntityClassName_TableName",
                table: "CodeEntityMappings",
                columns: new[] { "RepositoryId", "EntityClassName", "TableName" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseColumns_DatabaseTableId_Name",
                table: "DatabaseColumns",
                columns: new[] { "DatabaseTableId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_ApplicationId",
                table: "DatabaseConnections",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseRelationships_DatabaseConnectionId",
                table: "DatabaseRelationships",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseRelationships_FromTableId",
                table: "DatabaseRelationships",
                column: "FromTableId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseRelationships_ToTableId",
                table: "DatabaseRelationships",
                column: "ToTableId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseTables_DatabaseConnectionId_SchemaName_TableName",
                table: "DatabaseTables",
                columns: new[] { "DatabaseConnectionId", "SchemaName", "TableName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefactorPlans_DatabaseConnectionId",
                table: "RefactorPlans",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefactorPlans_EnhancementRequestId",
                table: "RefactorPlans",
                column: "EnhancementRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RefactorPlans_RepositoryId",
                table: "RefactorPlans",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaDriftFindings_DatabaseConnectionId_IsResolved",
                table: "SchemaDriftFindings",
                columns: new[] { "DatabaseConnectionId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaDriftFindings_RepositoryId",
                table: "SchemaDriftFindings",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemGraphEdges_SourceNodeId_TargetNodeId_EdgeType",
                table: "SystemGraphEdges",
                columns: new[] { "SourceNodeId", "TargetNodeId", "EdgeType" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemGraphEdges_TargetNodeId",
                table: "SystemGraphEdges",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemGraphNodes_ApplicationId",
                table: "SystemGraphNodes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemGraphNodes_ReferenceKey",
                table: "SystemGraphNodes",
                column: "ReferenceKey");

            migrationBuilder.CreateIndex(
                name: "IX_SystemGraphNodes_RepositoryId",
                table: "SystemGraphNodes",
                column: "RepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CodeEntityMappings");
            migrationBuilder.DropTable(name: "CodeTableReferences");
            migrationBuilder.DropTable(name: "DatabaseColumns");
            migrationBuilder.DropTable(name: "DatabaseRelationships");
            migrationBuilder.DropTable(name: "RefactorPlans");
            migrationBuilder.DropTable(name: "SchemaDriftFindings");
            migrationBuilder.DropTable(name: "SystemGraphEdges");
            migrationBuilder.DropTable(name: "DatabaseTables");
            migrationBuilder.DropTable(name: "SystemGraphNodes");
            migrationBuilder.DropTable(name: "DatabaseConnections");
        }
    }
}
