using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.BillingEmail).HasMaxLength(320);
    }
}

public class TenantUsageSnapshotConfiguration : IEntityTypeConfiguration<TenantUsageSnapshot>
{
    public void Configure(EntityTypeBuilder<TenantUsageSnapshot> builder)
    {
        builder.ToTable("TenantUsageSnapshots");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.PeriodStart }).IsUnique();
        builder.HasOne(x => x.Tenant)
            .WithMany(t => t.UsageSnapshots)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
