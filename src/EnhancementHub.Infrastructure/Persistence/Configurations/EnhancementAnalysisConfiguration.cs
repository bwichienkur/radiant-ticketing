using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class EnhancementAnalysisConfiguration : IEntityTypeConfiguration<EnhancementAnalysis>
{
    public void Configure(EntityTypeBuilder<EnhancementAnalysis> builder)
    {
        builder.ToTable("EnhancementAnalyses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureSummary).HasMaxLength(4000);
        builder.Property(x => x.BusinessRequirement).HasMaxLength(4000);
        builder.Property(x => x.TechnicalRequirements).HasMaxLength(8000);
        builder.Property(x => x.RiskExplanation).HasMaxLength(4000);
        builder.Property(x => x.TestingPlan).HasMaxLength(4000);
        builder.Property(x => x.RolloutPlan).HasMaxLength(4000);
        builder.Property(x => x.RollbackPlan).HasMaxLength(4000);
        builder.Property(x => x.OpenQuestions).HasMaxLength(4000);
        builder.Property(x => x.ApprovalChecklist).HasMaxLength(4000);
        builder.Property(x => x.FeatureCategory).HasMaxLength(200);
        builder.Property(x => x.BusinessGoal).HasMaxLength(1000);
        builder.Property(x => x.AmbiguityNotes).HasMaxLength(4000);
        builder.HasIndex(x => new { x.EnhancementRequestId, x.Version });
        builder.HasOne(x => x.EnhancementRequest)
            .WithMany(r => r.Analyses)
            .HasForeignKey(x => x.EnhancementRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
