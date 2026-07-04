using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class IndexedFileConfiguration : IEntityTypeConfiguration<IndexedFile>
{
    public void Configure(EntityTypeBuilder<IndexedFile> builder)
    {
        builder.ToTable("IndexedFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FilePath).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.Project).HasMaxLength(256);
        builder.Property(x => x.Language).HasMaxLength(64);
        builder.Property(x => x.FileType).HasMaxLength(64);
        builder.Property(x => x.Namespace).HasMaxLength(512);
        builder.Property(x => x.ClassName).HasMaxLength(256);
        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.Property(x => x.CommitHash).HasMaxLength(128);
        builder.Property(x => x.EmbeddingVector)
            .HasConversion(new FloatArrayToBytesConverter())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<float[]?>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                v => v == null ? 0 : v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                v => v == null ? null : v.ToArray()));
        builder.HasIndex(x => new { x.RepositoryId, x.BranchId, x.FilePath }).IsUnique();
        builder.HasOne(x => x.Repository)
            .WithMany(r => r.IndexedFiles)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Branch)
            .WithMany(b => b.IndexedFiles)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
