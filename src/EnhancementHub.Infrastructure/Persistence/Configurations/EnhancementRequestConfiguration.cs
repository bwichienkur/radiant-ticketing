using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class EnhancementRequestConfiguration : IEntityTypeConfiguration<EnhancementRequest>
{
    public void Configure(EntityTypeBuilder<EnhancementRequest> builder)
    {
        builder.ToTable("EnhancementRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BusinessDescription).IsRequired();
        builder.Property(x => x.DesiredOutcome).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.SupportingNotes).HasMaxLength(4000);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.TargetApplication)
            .WithMany(a => a.TargetedRequests)
            .HasForeignKey(x => x.TargetApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.SubmittedByUser)
            .WithMany(u => u.SubmittedRequests)
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Team)
            .WithMany(t => t.EnhancementRequests)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
