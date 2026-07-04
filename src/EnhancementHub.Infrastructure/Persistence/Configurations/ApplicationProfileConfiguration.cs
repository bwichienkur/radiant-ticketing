using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class ApplicationProfileConfiguration : IEntityTypeConfiguration<ApplicationProfile>
{
    public void Configure(EntityTypeBuilder<ApplicationProfile> builder)
    {
        builder.ToTable("ApplicationProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).HasMaxLength(2000);
        builder.Property(x => x.BusinessDomain).HasMaxLength(500);
        builder.Property(x => x.KeyComponents).HasMaxLength(4000);
        builder.Property(x => x.DatabaseUsage).HasMaxLength(4000);
        builder.Property(x => x.ExternalIntegrations).HasMaxLength(4000);
        builder.Property(x => x.InternalDependencies).HasMaxLength(4000);
        builder.Property(x => x.DeploymentNotes).HasMaxLength(4000);
        builder.Property(x => x.RiskSensitiveAreas).HasMaxLength(4000);
        builder.Property(x => x.OwnershipMetadata).HasMaxLength(2000);
        builder.HasIndex(x => new { x.ApplicationId, x.RepositoryId });
        builder.HasOne(x => x.Application)
            .WithMany(a => a.Profiles)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.Profiles)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
