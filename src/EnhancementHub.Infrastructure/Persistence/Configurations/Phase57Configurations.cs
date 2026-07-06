using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinitions");
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OptionsJson).HasMaxLength(4000);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public sealed class EnhancementRequestCustomFieldValueConfiguration
    : IEntityTypeConfiguration<EnhancementRequestCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<EnhancementRequestCustomFieldValue> builder)
    {
        builder.ToTable("EnhancementRequestCustomFieldValues");
        builder.Property(x => x.TextValue).HasMaxLength(4000);
        builder.HasIndex(x => new { x.EnhancementRequestId, x.CustomFieldDefinitionId }).IsUnique();
        builder.HasOne(x => x.EnhancementRequest)
            .WithMany(r => r.CustomFieldValues)
            .HasForeignKey(x => x.EnhancementRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Definition)
            .WithMany(d => d.Values)
            .HasForeignKey(x => x.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UserValue)
            .WithMany()
            .HasForeignKey(x => x.UserValueId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
