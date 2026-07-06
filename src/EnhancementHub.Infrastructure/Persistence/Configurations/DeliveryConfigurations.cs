using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class TenantDeliveryProfileConfiguration : IEntityTypeConfiguration<TenantDeliveryProfile>
{
    public void Configure(EntityTypeBuilder<TenantDeliveryProfile> builder)
    {
        builder.ToTable("TenantDeliveryProfiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.Property(x => x.VaultSecretPrefix).HasMaxLength(500);
        builder.Property(x => x.ChangeWindowNotes).HasMaxLength(4000);
        builder.HasOne(x => x.Tenant)
            .WithOne(t => t.DeliveryProfile)
            .HasForeignKey<TenantDeliveryProfile>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TenantDeploymentEnvironmentConfiguration : IEntityTypeConfiguration<TenantDeploymentEnvironment>
{
    public void Configure(EntityTypeBuilder<TenantDeploymentEnvironment> builder)
    {
        builder.ToTable("TenantDeploymentEnvironments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUrlTemplate).HasMaxLength(500);
        builder.Property(x => x.SecretReferencePrefix).HasMaxLength(500);
        builder.HasOne(x => x.Tenant)
            .WithMany(t => t.DeploymentEnvironments)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationDeliveryProfileConfiguration : IEntityTypeConfiguration<ApplicationDeliveryProfile>
{
    public void Configure(EntityTypeBuilder<ApplicationDeliveryProfile> builder)
    {
        builder.ToTable("ApplicationDeliveryProfiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ApplicationId).IsUnique();
        builder.Property(x => x.BranchNamingPattern).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CicdPipelineReference).HasMaxLength(500);
        builder.Property(x => x.SmokeTestPath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ConfigTransformsJson).HasColumnType("TEXT");
        builder.Property(x => x.ConnectionMappingsJson).HasColumnType("TEXT");
        builder.HasOne(x => x.Application)
            .WithOne(a => a.DeliveryProfile)
            .HasForeignKey<ApplicationDeliveryProfile>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PrimaryRepository)
            .WithMany()
            .HasForeignKey(x => x.PrimaryRepositoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
