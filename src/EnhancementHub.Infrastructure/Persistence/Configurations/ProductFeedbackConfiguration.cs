using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public sealed class ProductFeedbackConfiguration : IEntityTypeConfiguration<ProductFeedback>
{
    public void Configure(EntityTypeBuilder<ProductFeedback> builder)
    {
        builder.ToTable("ProductFeedbacks");
        builder.Property(x => x.WorkflowKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.HasIndex(x => new { x.WorkflowKey, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.WorkflowKey, x.CreatedAt });
        builder.HasOne(x => x.User)
            .WithMany(u => u.ProductFeedbacks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
