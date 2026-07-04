using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class DatabaseConnectionConfiguration : IEntityTypeConfiguration<DatabaseConnection>
{
    public void Configure(EntityTypeBuilder<DatabaseConnection> builder)
    {
        builder.ToTable("DatabaseConnections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConnectionStringProtected).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(256);
        builder.Property(x => x.DatabaseName).HasMaxLength(256);
        builder.Property(x => x.ScanStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScanError).HasMaxLength(4000);
        builder.HasIndex(x => x.ApplicationId);
        builder.HasOne(x => x.Application)
            .WithMany(a => a.DatabaseConnections)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DatabaseTableConfiguration : IEntityTypeConfiguration<DatabaseTable>
{
    public void Configure(EntityTypeBuilder<DatabaseTable> builder)
    {
        builder.ToTable("DatabaseTables");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SchemaName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TableName).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.DatabaseConnectionId, x.SchemaName, x.TableName }).IsUnique();
        builder.HasOne(x => x.DatabaseConnection)
            .WithMany(c => c.Tables)
            .HasForeignKey(x => x.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DatabaseColumnConfiguration : IEntityTypeConfiguration<DatabaseColumn>
{
    public void Configure(EntityTypeBuilder<DatabaseColumn> builder)
    {
        builder.ToTable("DatabaseColumns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(256).IsRequired();
        builder.HasOne(x => x.DatabaseTable)
            .WithMany(t => t.Columns)
            .HasForeignKey(x => x.DatabaseTableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DatabaseRelationshipConfiguration : IEntityTypeConfiguration<DatabaseRelationship>
{
    public void Configure(EntityTypeBuilder<DatabaseRelationship> builder)
    {
        builder.ToTable("DatabaseRelationships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromColumnName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ToColumnName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RelationshipType).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.DatabaseConnection)
            .WithMany(c => c.Relationships)
            .HasForeignKey(x => x.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FromTable)
            .WithMany(t => t.OutgoingRelationships)
            .HasForeignKey(x => x.FromTableId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToTable)
            .WithMany(t => t.IncomingRelationships)
            .HasForeignKey(x => x.ToTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SystemGraphNodeConfiguration : IEntityTypeConfiguration<SystemGraphNode>
{
    public void Configure(EntityTypeBuilder<SystemGraphNode> builder)
    {
        builder.ToTable("SystemGraphNodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ReferenceKey).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => new { x.ApplicationId, x.ReferenceKey });
        builder.HasOne(x => x.Application)
            .WithMany(a => a.GraphNodes)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.GraphNodes)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SystemGraphEdgeConfiguration : IEntityTypeConfiguration<SystemGraphEdge>
{
    public void Configure(EntityTypeBuilder<SystemGraphEdge> builder)
    {
        builder.ToTable("SystemGraphEdges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(256);
        builder.HasOne(x => x.SourceNode)
            .WithMany(n => n.OutgoingEdges)
            .HasForeignKey(x => x.SourceNodeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.TargetNode)
            .WithMany(n => n.IncomingEdges)
            .HasForeignKey(x => x.TargetNodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SchemaDriftFindingConfiguration : IEntityTypeConfiguration<SchemaDriftFinding>
{
    public void Configure(EntityTypeBuilder<SchemaDriftFinding> builder)
    {
        builder.ToTable("SchemaDriftFindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CodeReference).HasMaxLength(512);
        builder.Property(x => x.DatabaseReference).HasMaxLength(512);
        builder.HasIndex(x => x.DatabaseConnectionId);
        builder.HasOne(x => x.DatabaseConnection)
            .WithMany(c => c.DriftFindings)
            .HasForeignKey(x => x.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.DriftFindings)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SchemaDriftReportConfiguration : IEntityTypeConfiguration<SchemaDriftReport>
{
    public void Configure(EntityTypeBuilder<SchemaDriftReport> builder)
    {
        builder.ToTable("SchemaDriftReports");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Connection)
            .WithMany()
            .HasForeignKey(x => x.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Repository)
            .WithMany()
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SystemGraphSnapshotConfiguration : IEntityTypeConfiguration<SystemGraphSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemGraphSnapshot> builder)
    {
        builder.ToTable("SystemGraphSnapshots");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Application)
            .WithMany(a => a.SystemGraphSnapshots)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CodeEntityMappingConfiguration : IEntityTypeConfiguration<CodeEntityMapping>
{
    public void Configure(EntityTypeBuilder<CodeEntityMapping> builder)
    {
        builder.ToTable("CodeEntityMappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityClassName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EntityNamespace).HasMaxLength(512).IsRequired();
        builder.Property(x => x.EntityFilePath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.TableName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SchemaName).HasMaxLength(128).IsRequired();
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.EntityMappings)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CodeTableReferenceConfiguration : IEntityTypeConfiguration<CodeTableReference>
{
    public void Configure(EntityTypeBuilder<CodeTableReference> builder)
    {
        builder.ToTable("CodeTableReferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourcePath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.TableName).HasMaxLength(256).IsRequired();
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.TableReferences)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefactorPlanConfiguration : IEntityTypeConfiguration<RefactorPlan>
{
    public void Configure(EntityTypeBuilder<RefactorPlan> builder)
    {
        builder.ToTable("RefactorPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TargetDescription).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.DatabaseConnection)
            .WithMany(c => c.RefactorPlans)
            .HasForeignKey(x => x.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.RefactorPlans)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OnPremAgentConfiguration : IEntityTypeConfiguration<OnPremAgent>
{
    public void Configure(EntityTypeBuilder<OnPremAgent> builder)
    {
        builder.ToTable("OnPremAgents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ApiKeyHash).HasMaxLength(256).IsRequired();
        builder.HasOne(x => x.Application)
            .WithMany(a => a.OnPremAgents)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
