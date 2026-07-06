using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class ApplicationTestSuiteConfiguration : IEntityTypeConfiguration<ApplicationTestSuite>
{
    public void Configure(EntityTypeBuilder<ApplicationTestSuite> builder)
    {
        builder.ToTable("ApplicationTestSuites");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ApplicationId, x.Name }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne(x => x.Application)
            .WithMany(a => a.TestSuites)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("TestCases");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TestSuiteId, x.Title });
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.RepositoryPath).HasMaxLength(500);
        builder.Property(x => x.StepsJson).HasColumnType("TEXT");
        builder.Property(x => x.TagsJson).HasMaxLength(1000);
        builder.HasOne(x => x.TestSuite)
            .WithMany(s => s.TestCases)
            .HasForeignKey(x => x.TestSuiteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SourceEnhancementRequest)
            .WithMany()
            .HasForeignKey(x => x.SourceEnhancementRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TestCaseVersionConfiguration : IEntityTypeConfiguration<TestCaseVersion>
{
    public void Configure(EntityTypeBuilder<TestCaseVersion> builder)
    {
        builder.ToTable("TestCaseVersions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TestCaseId, x.Version }).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.StepsJson).HasColumnType("TEXT");
        builder.HasOne(x => x.TestCase)
            .WithMany(c => c.Versions)
            .HasForeignKey(x => x.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeliveryRunTestResultConfiguration : IEntityTypeConfiguration<DeliveryRunTestResult>
{
    public void Configure(EntityTypeBuilder<DeliveryRunTestResult> builder)
    {
        builder.ToTable("DeliveryRunTestResults");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EnhancementDeliveryRunId, x.TestCaseId });
        builder.Property(x => x.TestCaseTitle).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(4000);
        builder.Property(x => x.ScreenshotStoragePath).HasMaxLength(1000);
        builder.HasOne(x => x.DeliveryRun)
            .WithMany(r => r.TestResults)
            .HasForeignKey(x => x.EnhancementDeliveryRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.TestCase)
            .WithMany()
            .HasForeignKey(x => x.TestCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TestCaseVersion)
            .WithMany()
            .HasForeignKey(x => x.TestCaseVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
