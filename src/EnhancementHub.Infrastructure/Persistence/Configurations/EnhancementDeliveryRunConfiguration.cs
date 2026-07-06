using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class EnhancementDeliveryRunConfiguration : IEntityTypeConfiguration<EnhancementDeliveryRun>
{
    public void Configure(EntityTypeBuilder<EnhancementDeliveryRun> builder)
    {
        builder.ToTable("EnhancementDeliveryRuns");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EnhancementRequestId, x.RunNumber }).IsUnique();
        builder.Property(x => x.BranchName).HasMaxLength(300);
        builder.Property(x => x.PullRequestUrl).HasMaxLength(1000);
        builder.Property(x => x.CommitSha).HasMaxLength(64);
        builder.Property(x => x.TestUrl).HasMaxLength(1000);
        builder.Property(x => x.TestDeployReference).HasMaxLength(500);
        builder.Property(x => x.QaStepsJson).HasColumnType("TEXT");
        builder.Property(x => x.QaVideoStoragePath).HasMaxLength(1000);
        builder.Property(x => x.QaReportStoragePath).HasMaxLength(1000);
        builder.Property(x => x.UatNotes).HasMaxLength(4000);
        builder.Property(x => x.ProdDeployReference).HasMaxLength(500);
        builder.Property(x => x.ProdArtifactReference).HasMaxLength(500);
        builder.Property(x => x.RollbackTargetDeployReference).HasMaxLength(500);
        builder.Property(x => x.RollbackTargetCommitSha).HasMaxLength(64);
        builder.Property(x => x.TimelineJson).HasColumnType("TEXT");
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.HasOne(x => x.EnhancementRequest)
            .WithMany(r => r.DeliveryRuns)
            .HasForeignKey(x => x.EnhancementRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
