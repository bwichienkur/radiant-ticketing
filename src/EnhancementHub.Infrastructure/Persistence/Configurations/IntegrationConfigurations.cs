using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class OpenApiRegistrationConfiguration : IEntityTypeConfiguration<OpenApiRegistration>
{
    public void Configure(EntityTypeBuilder<OpenApiRegistration> builder)
    {
        builder.ToTable("OpenApiRegistrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ApplicationId);
        builder.HasOne(x => x.Application)
            .WithMany(a => a.OpenApiRegistrations)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OpenApiEndpointConfiguration : IEntityTypeConfiguration<OpenApiEndpoint>
{
    public void Configure(EntityTypeBuilder<OpenApiEndpoint> builder)
    {
        builder.ToTable("OpenApiEndpoints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Path).HasMaxLength(512).IsRequired();
        builder.Property(x => x.HttpMethod).HasMaxLength(16).IsRequired();
        builder.Property(x => x.OperationId).HasMaxLength(256);
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.Tags).HasMaxLength(512);
        builder.HasIndex(x => new { x.OpenApiRegistrationId, x.Path, x.HttpMethod });
        builder.HasOne(x => x.Registration)
            .WithMany(r => r.Endpoints)
            .HasForeignKey(x => x.OpenApiRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
