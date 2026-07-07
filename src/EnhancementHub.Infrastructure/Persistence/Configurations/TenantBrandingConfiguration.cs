using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.ToTable("TenantBrandings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LogoUrl).HasMaxLength(2048);
        builder.Property(x => x.AccentColor).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(120);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne(x => x.Tenant)
            .WithOne(t => t.Branding)
            .HasForeignKey<TenantBranding>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
