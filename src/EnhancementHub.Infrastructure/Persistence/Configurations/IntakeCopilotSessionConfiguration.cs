using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public sealed class IntakeCopilotSessionConfiguration : IEntityTypeConfiguration<IntakeCopilotSession>
{
    public void Configure(EntityTypeBuilder<IntakeCopilotSession> builder)
    {
        builder.ToTable("IntakeCopilotSessions");
        builder.Property(x => x.MessagesJson).IsRequired();
        builder.Property(x => x.DraftJson).HasMaxLength(8000);
        builder.Property(x => x.LastAssistantMessage).HasMaxLength(4000);
        builder.Property(x => x.PolicySourceLabel).HasMaxLength(512);
        builder.Property(x => x.PolicySourceText).HasMaxLength(50000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SuggestedTemplate)
            .WithMany()
            .HasForeignKey(x => x.SuggestedTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.CreatedRequest)
            .WithMany()
            .HasForeignKey(x => x.CreatedRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
