using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Infrastructure.Persistence.Configurations;

public class EntityConfigurations : IEntityTypeConfiguration<Team>,
    IEntityTypeConfiguration<TeamMember>,
    IEntityTypeConfiguration<ApplicationEntity>,
    IEntityTypeConfiguration<RepositoryBranch>,
    IEntityTypeConfiguration<EnhancementAttachment>,
    IEntityTypeConfiguration<AnalysisFinding>,
    IEntityTypeConfiguration<AffectedApplication>,
    IEntityTypeConfiguration<AffectedRepository>,
    IEntityTypeConfiguration<AffectedComponent>,
    IEntityTypeConfiguration<DatabaseChangeRecommendation>,
    IEntityTypeConfiguration<ApiChangeRecommendation>,
    IEntityTypeConfiguration<RiskAssessment>,
    IEntityTypeConfiguration<ApprovalAction>,
    IEntityTypeConfiguration<Comment>,
    IEntityTypeConfiguration<ExternalTicket>,
    IEntityTypeConfiguration<IndexedSymbol>,
    IEntityTypeConfiguration<AiPromptConfiguration>,
    IEntityTypeConfiguration<AiPromptRun>,
    IEntityTypeConfiguration<RetrievedContextItem>,
    IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }

    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");
        builder.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
        builder.HasOne(x => x.Team).WithMany(t => t.Members).HasForeignKey(x => x.TeamId);
        builder.HasOne(x => x.User).WithMany(u => u.TeamMemberships).HasForeignKey(x => x.UserId);
    }

    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("Applications");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(x => x.OwnerTeam).WithMany(t => t.OwnedApplications).HasForeignKey(x => x.OwnerTeamId);
    }

    public void Configure(EntityTypeBuilder<RepositoryBranch> builder)
    {
        builder.ToTable("RepositoryBranches");
        builder.Property(x => x.BranchName).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.RepositoryId, x.BranchName }).IsUnique();
        builder.HasOne(x => x.Repository).WithMany(r => r.Branches).HasForeignKey(x => x.RepositoryId);
    }

    public void Configure(EntityTypeBuilder<EnhancementAttachment> builder)
    {
        builder.ToTable("EnhancementAttachments");
        builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        builder.HasOne(x => x.EnhancementRequest).WithMany(r => r.Attachments).HasForeignKey(x => x.EnhancementRequestId);
    }

    public void Configure(EntityTypeBuilder<AnalysisFinding> builder)
    {
        builder.ToTable("AnalysisFindings");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.Findings).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<AffectedApplication> builder)
    {
        builder.ToTable("AffectedApplications");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.AffectedApplications).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<AffectedRepository> builder)
    {
        builder.ToTable("AffectedRepositories");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.AffectedRepositories).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<AffectedComponent> builder)
    {
        builder.ToTable("AffectedComponents");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.AffectedComponents).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<DatabaseChangeRecommendation> builder)
    {
        builder.ToTable("DatabaseChangeRecommendations");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.DatabaseChangeRecommendations).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<ApiChangeRecommendation> builder)
    {
        builder.ToTable("ApiChangeRecommendations");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.ApiChangeRecommendations).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<RiskAssessment> builder)
    {
        builder.ToTable("RiskAssessments");
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.RiskAssessments).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<ApprovalAction> builder)
    {
        builder.ToTable("ApprovalActions");
        builder.HasOne(x => x.EnhancementRequest).WithMany(r => r.ApprovalActions).HasForeignKey(x => x.EnhancementRequestId);
    }

    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasOne(x => x.EnhancementRequest).WithMany(r => r.Comments).HasForeignKey(x => x.EnhancementRequestId);
    }

    public void Configure(EntityTypeBuilder<ExternalTicket> builder)
    {
        builder.ToTable("ExternalTickets");
        builder.Property(x => x.ExternalId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ExternalUrl).HasMaxLength(2048).IsRequired();
        builder.HasIndex(x => new { x.Provider, x.ExternalId });
        builder.HasOne(x => x.EnhancementRequest).WithMany(r => r.ExternalTickets).HasForeignKey(x => x.EnhancementRequestId);
        builder.HasOne(x => x.CreatedByUser).WithMany(u => u.CreatedExternalTickets).HasForeignKey(x => x.CreatedByUserId);
    }

    public void Configure(EntityTypeBuilder<IndexedSymbol> builder)
    {
        builder.ToTable("IndexedSymbols");
        builder.HasOne(x => x.IndexedFile).WithMany(f => f.Symbols).HasForeignKey(x => x.IndexedFileId);
    }

    public void Configure(EntityTypeBuilder<AiPromptConfiguration> builder)
    {
        builder.ToTable("AiPromptConfigurations");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.Name, x.Version }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<AiPromptRun> builder)
    {
        builder.ToTable("AiPromptRuns");
        builder.HasOne(x => x.EnhancementRequest).WithMany(r => r.AiPromptRuns).HasForeignKey(x => x.EnhancementRequestId);
        builder.HasOne(x => x.EnhancementAnalysis).WithMany(a => a.AiPromptRuns).HasForeignKey(x => x.EnhancementAnalysisId);
    }

    public void Configure(EntityTypeBuilder<RetrievedContextItem> builder)
    {
        builder.ToTable("RetrievedContextItems");
        builder.HasOne(x => x.AiPromptRun).WithMany(r => r.RetrievedContextItems).HasForeignKey(x => x.AiPromptRunId);
    }

    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.Property(x => x.Key).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
