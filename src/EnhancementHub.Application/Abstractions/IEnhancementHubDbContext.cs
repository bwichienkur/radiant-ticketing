using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Abstractions;

public interface IEnhancementHubDbContext
{
    DbSet<User> Users { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<ApplicationEntity> Applications { get; }
    DbSet<Repository> Repositories { get; }
    DbSet<RepositoryBranch> RepositoryBranches { get; }
    DbSet<EnhancementRequest> EnhancementRequests { get; }
    DbSet<EnhancementAttachment> EnhancementAttachments { get; }
    DbSet<EnhancementAnalysis> EnhancementAnalyses { get; }
    DbSet<AnalysisFinding> AnalysisFindings { get; }
    DbSet<AffectedApplication> AffectedApplications { get; }
    DbSet<AffectedRepository> AffectedRepositories { get; }
    DbSet<AffectedComponent> AffectedComponents { get; }
    DbSet<DatabaseChangeRecommendation> DatabaseChangeRecommendations { get; }
    DbSet<ApiChangeRecommendation> ApiChangeRecommendations { get; }
    DbSet<RiskAssessment> RiskAssessments { get; }
    DbSet<ApprovalAction> ApprovalActions { get; }
    DbSet<Comment> Comments { get; }
    DbSet<ExternalTicket> ExternalTickets { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<IndexedFile> IndexedFiles { get; }
    DbSet<IndexedSymbol> IndexedSymbols { get; }
    DbSet<ApplicationProfile> ApplicationProfiles { get; }
    DbSet<AiPromptConfiguration> AiPromptConfigurations { get; }
    DbSet<AiPromptRun> AiPromptRuns { get; }
    DbSet<RetrievedContextItem> RetrievedContextItems { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<DatabaseConnection> DatabaseConnections { get; }
    DbSet<DatabaseTable> DatabaseTables { get; }
    DbSet<DatabaseColumn> DatabaseColumns { get; }
    DbSet<DatabaseRelationship> DatabaseRelationships { get; }
    DbSet<CodeEntityMapping> CodeEntityMappings { get; }
    DbSet<CodeTableReference> CodeTableReferences { get; }
    DbSet<SchemaDriftFinding> SchemaDriftFindings { get; }
    DbSet<SystemGraphNode> SystemGraphNodes { get; }
    DbSet<SystemGraphEdge> SystemGraphEdges { get; }
    DbSet<RefactorPlan> RefactorPlans { get; }
    DbSet<SystemGraphSnapshot> SystemGraphSnapshots { get; }
    DbSet<SchemaDriftReport> SchemaDriftReports { get; }
    DbSet<OnPremAgent> OnPremAgents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
