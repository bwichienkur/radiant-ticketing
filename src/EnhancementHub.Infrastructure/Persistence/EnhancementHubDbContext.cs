using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Infrastructure.Persistence;

public class EnhancementHubDbContext : DbContext, IEnhancementHubDbContext
{
    public EnhancementHubDbContext(DbContextOptions<EnhancementHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryBranch> RepositoryBranches => Set<RepositoryBranch>();
    public DbSet<EnhancementRequest> EnhancementRequests => Set<EnhancementRequest>();
    public DbSet<EnhancementAttachment> EnhancementAttachments => Set<EnhancementAttachment>();
    public DbSet<EnhancementAnalysis> EnhancementAnalyses => Set<EnhancementAnalysis>();
    public DbSet<AnalysisFinding> AnalysisFindings => Set<AnalysisFinding>();
    public DbSet<AffectedApplication> AffectedApplications => Set<AffectedApplication>();
    public DbSet<AffectedRepository> AffectedRepositories => Set<AffectedRepository>();
    public DbSet<AffectedComponent> AffectedComponents => Set<AffectedComponent>();
    public DbSet<DatabaseChangeRecommendation> DatabaseChangeRecommendations => Set<DatabaseChangeRecommendation>();
    public DbSet<ApiChangeRecommendation> ApiChangeRecommendations => Set<ApiChangeRecommendation>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ExternalTicket> ExternalTickets => Set<ExternalTicket>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IndexedFile> IndexedFiles => Set<IndexedFile>();
    public DbSet<IndexedSymbol> IndexedSymbols => Set<IndexedSymbol>();
    public DbSet<ApplicationProfile> ApplicationProfiles => Set<ApplicationProfile>();
    public DbSet<AiPromptConfiguration> AiPromptConfigurations => Set<AiPromptConfiguration>();
    public DbSet<AiPromptRun> AiPromptRuns => Set<AiPromptRun>();
    public DbSet<RetrievedContextItem> RetrievedContextItems => Set<RetrievedContextItem>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<DatabaseConnection> DatabaseConnections => Set<DatabaseConnection>();
    public DbSet<DatabaseTable> DatabaseTables => Set<DatabaseTable>();
    public DbSet<DatabaseColumn> DatabaseColumns => Set<DatabaseColumn>();
    public DbSet<DatabaseRelationship> DatabaseRelationships => Set<DatabaseRelationship>();
    public DbSet<CodeEntityMapping> CodeEntityMappings => Set<CodeEntityMapping>();
    public DbSet<CodeEntityProperty> CodeEntityProperties => Set<CodeEntityProperty>();
    public DbSet<CodeTableReference> CodeTableReferences => Set<CodeTableReference>();
    public DbSet<SchemaDriftFinding> SchemaDriftFindings => Set<SchemaDriftFinding>();
    public DbSet<SystemGraphNode> SystemGraphNodes => Set<SystemGraphNode>();
    public DbSet<SystemGraphEdge> SystemGraphEdges => Set<SystemGraphEdge>();
    public DbSet<RefactorPlan> RefactorPlans => Set<RefactorPlan>();
    public DbSet<SystemGraphSnapshot> SystemGraphSnapshots => Set<SystemGraphSnapshot>();
    public DbSet<SchemaDriftReport> SchemaDriftReports => Set<SchemaDriftReport>();
    public DbSet<OnPremAgent> OnPremAgents => Set<OnPremAgent>();
    public DbSet<OnboardingSession> OnboardingSessions => Set<OnboardingSession>();
    public DbSet<ServiceApiKey> ServiceApiKeys => Set<ServiceApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnhancementHubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
