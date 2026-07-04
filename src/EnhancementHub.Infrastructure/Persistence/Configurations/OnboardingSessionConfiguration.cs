using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public sealed class OnboardingSessionConfiguration : IEntityTypeConfiguration<OnboardingSession>
{
    public void Configure(EntityTypeBuilder<OnboardingSession> builder)
    {
        builder.ToTable("OnboardingSessions");
        builder.Property(x => x.DiscoveryStatus).HasMaxLength(4000);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => x.StartedByUserId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Application)
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
