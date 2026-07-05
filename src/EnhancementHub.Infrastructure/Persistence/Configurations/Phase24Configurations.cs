using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class ApprovalPolicyRuleConfiguration : IEntityTypeConfiguration<ApprovalPolicyRule>
{
    public void Configure(EntityTypeBuilder<ApprovalPolicyRule> builder)
    {
        builder.ToTable("ApprovalPolicyRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.Priority);
    }
}

public class EnhancementTemplateConfiguration : IEntityTypeConfiguration<EnhancementTemplate>
{
    public void Configure(EntityTypeBuilder<EnhancementTemplate> builder)
    {
        builder.ToTable("EnhancementTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DomainCategory).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.DomainCategory);
    }
}
