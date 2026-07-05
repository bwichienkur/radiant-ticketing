using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public sealed class ServiceApiKeyConfiguration : IEntityTypeConfiguration<ServiceApiKey>
{
    public void Configure(EntityTypeBuilder<ServiceApiKey> builder)
    {
        builder.ToTable("ServiceApiKeys");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.KeyPrefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.KeyPrefix);
        builder.HasOne(x => x.ServiceUser)
            .WithMany()
            .HasForeignKey(x => x.ServiceUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
