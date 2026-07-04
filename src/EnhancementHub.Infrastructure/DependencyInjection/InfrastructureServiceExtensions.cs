using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Infrastructure.Background;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Persistence.Repositories;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace EnhancementHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public const string OpenAiHttpClientName = "OpenAI";
    public const string ExternalTicketsHttpClientName = "ExternalTickets";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerBackgroundJobs = false)
    {
        services.AddHttpContextAccessor();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=enhancementhub.db";
        var provider = ResolveDatabaseProvider(configuration, connectionString);

        services.AddDbContext<EnhancementHubDbContext>(options =>
        {
            if (provider == DatabaseProvider.PostgreSql)
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IEnhancementHubDbContext>(sp => sp.GetRequiredService<EnhancementHubDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.AddDataProtection();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<IConnectionStringProtector>(sp => sp.GetRequiredService<ISecretProtector>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IPasswordHasher, DevPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<InMemoryVectorSearchService>();
        services.AddScoped<PgVectorSearchService>();
        services.AddScoped<IVectorSearchService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var vectorProvider = config["VectorSearch:Provider"];
            var databaseProvider = config["Database:Provider"];
            if (string.Equals(vectorProvider, "PgVector", StringComparison.OrdinalIgnoreCase)
                && string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<PgVectorSearchService>();
            }

            return sp.GetRequiredService<InMemoryVectorSearchService>();
        });
        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<S3FileStorageService>();
        services.AddScoped<IFileStorageService>(sp =>
        {
            var provider = configuration["Storage:Provider"] ?? "Local";
            return string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<S3FileStorageService>()
                : sp.GetRequiredService<LocalFileStorageService>();
        });
        services.AddScoped<INotificationPublisher, NoOpNotificationPublisher>();
        services.AddScoped<IKnowledgeSearchService, KeywordKnowledgeSearchService>();
        services.AddScoped<IGitRepositoryScanner, RoslynRepositoryScanner>();
        services.AddScoped<EfEntityTableMapper>();
        services.AddScoped<SqlServerSchemaScanner>();
        services.AddScoped<PostgreSqlSchemaScanner>();
        services.AddScoped<DatabaseSchemaScannerFactory>();
        services.AddScoped<DatabaseSchemaIngestionService>();
        services.AddScoped<ISystemGraphBuilder, SystemGraphBuilderService>();
        services.AddScoped<ISchemaDriftDetector, SchemaDriftDetectorService>();
        services.AddScoped<IDocumentationExportService, DocumentationExportService>();
        services.AddScoped<IRefactorBlastRadiusService, RefactorBlastRadiusService>();
        services.AddScoped<IRefactorPlanGenerator, RefactorPlanGeneratorService>();
        services.AddScoped<IOnPremAgentService, OnPremAgentService>();
        services.AddScoped<ApplicationProfileGenerator>();
        services.AddScoped<IRepositoryIndexer, RepositoryIndexerService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddSingleton<PromptSanitizer>();
        services.AddSingleton<AiResponseValidator>();

        services.AddHttpClient(OpenAiHttpClientName)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(ExternalTicketsHttpClientName)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddScoped<IAiAnalysisService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new OpenAiAnalysisService(
                factory.CreateClient(OpenAiHttpClientName),
                configuration,
                sp.GetRequiredService<PromptSanitizer>(),
                sp.GetRequiredService<AiResponseValidator>(),
                sp.GetRequiredService<IRiskScoringService>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiAnalysisService>>());
        });

        services.AddScoped<IExternalTicketExporter, GitHubTicketExporter>(sp =>
            new GitHubTicketExporter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ExternalTicketsHttpClientName),
                configuration,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitHubTicketExporter>>()));

        services.AddScoped<IExternalTicketExporter, AzureDevOpsTicketExporter>(sp =>
            new AzureDevOpsTicketExporter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ExternalTicketsHttpClientName),
                configuration,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureDevOpsTicketExporter>>()));

        services.AddScoped<IExternalTicketExporter, JiraTicketExporter>(sp =>
            new JiraTicketExporter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ExternalTicketsHttpClientName),
                configuration,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JiraTicketExporter>>()));

        services.AddScoped<IExternalTicketExporterFactory, ExternalTicketExporterFactory>();
        services.AddScoped<IDatabaseSchemaScanner, DatabaseSchemaScanner>();
        services.AddScoped<IDatabaseSchemaIngestionService, DatabaseSchemaIngestionService>();
        services.AddScoped<IEfEntityTableMapper, EfEntityTableMapper>();

        if (registerBackgroundJobs)
        {
            services.AddHostedService<RepositoryIndexingJob>();
            services.AddHostedService<AiAnalysisJob>();
            services.AddHostedService<ScheduledRepositoryRefreshJob>();
            services.AddHostedService<DatabaseSchemaScanJob>();
        }

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static DatabaseProvider ResolveDatabaseProvider(IConfiguration configuration, string connectionString)
    {
        var configured = configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                || configured.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                || configured.Equals("Npgsql", StringComparison.OrdinalIgnoreCase)
                ? DatabaseProvider.PostgreSql
                : DatabaseProvider.Sqlite;
        }

        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseProvider.PostgreSql;
        }

        return DatabaseProvider.Sqlite;
    }

    private enum DatabaseProvider
    {
        Sqlite,
        PostgreSql
    }
}
