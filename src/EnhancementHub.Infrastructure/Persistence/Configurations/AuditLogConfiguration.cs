using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreviousValue).HasMaxLength(4000);
        builder.Property(x => x.NewValue).HasMaxLength(4000);
        builder.Property(x => x.Comments).HasMaxLength(4000);
        builder.Property(x => x.AiModelUsed).HasMaxLength(128);
        builder.Property(x => x.PromptVersion).HasMaxLength(64);
        builder.Property(x => x.RetrievedContextReferences).HasMaxLength(4000);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
