using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Infrastructure.Background;
using EnhancementHub.Infrastructure.Background.Executors;
using EnhancementHub.Infrastructure.Observability;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Persistence.Repositories;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.Integrations;
using EnhancementHub.Infrastructure.Services.Notifications;
using EnhancementHub.Infrastructure.Services.Webhooks;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;
using Polly.Extensions.Http;

namespace EnhancementHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public const string OpenAiHttpClientName = "OpenAI";
    public const string ExternalTicketsHttpClientName = "ExternalTickets";
    public const string QdrantHttpClientName = "Qdrant";
    public const string TeamsWebhookHttpClientName = "TeamsWebhook";
    public const string GitHubAppHttpClientName = "GitHubApp";
    public const string PolicyUrlFetcherHttpClientName = "PolicyUrlFetcher";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerBackgroundJobs = false,
        bool registerHangfireMonitoring = false)
    {
        services.AddHttpContextAccessor();

        var defaultConnectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=enhancementhub.db";
        var reportingConnectionString = configuration.GetConnectionString("Reporting") ?? defaultConnectionString;
        var provider = ResolveDatabaseProvider(configuration, defaultConnectionString);
        var scalingOptions = configuration
            .GetSection(Application.Options.DatabaseScalingOptions.SectionName)
            .Get<Application.Options.DatabaseScalingOptions>()
            ?? new Application.Options.DatabaseScalingOptions();

        services.Configure<Application.Options.DatabaseScalingOptions>(
            configuration.GetSection(Application.Options.DatabaseScalingOptions.SectionName));

        services.AddDbContext<EnhancementHubDbContext>((sp, options) =>
        {
            var connectionString = ResolveConnectionString(sp, defaultConnectionString);
            ConfigureDbContextOptions(options, connectionString, provider, scalingOptions);
            options.AddInterceptors(sp.GetRequiredService<TenantSearchPathConnectionInterceptor>());
        });

        services.AddDbContext<ReportingDbContext>((sp, options) =>
        {
            var connectionString = ResolveConnectionString(sp, reportingConnectionString);
            ConfigureDbContextOptions(options, connectionString, provider, scalingOptions);
        });
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ReportingDbContext>());

        services.AddScoped<IEnhancementHubDbContext>(sp => sp.GetRequiredService<EnhancementHubDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.ConfigureEnhancementHubDataProtection(configuration);

        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<IConnectionStringProtector>(sp => sp.GetRequiredService<ISecretProtector>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEnhancementRequestAccessService, EnhancementRequestAccessService>();
        services.AddScoped<IApplicationAccessService, ApplicationAccessService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<InMemoryVectorSearchService>();
        services.AddScoped<PgVectorSearchService>();
        services.AddScoped<QdrantVectorSearchService>();
        services.AddScoped<AzureSearchVectorService>();
        services.AddScoped<IVectorSearchService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var vectorProvider = config["VectorSearch:Provider"] ?? "InMemory";
            var databaseProvider = config["Database:Provider"];

            if (string.Equals(vectorProvider, "PgVector", StringComparison.OrdinalIgnoreCase)
                && string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<PgVectorSearchService>();
            }

            if (string.Equals(vectorProvider, "Qdrant", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<QdrantVectorSearchService>();
            }

            if (string.Equals(vectorProvider, "AzureSearch", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<AzureSearchVectorService>();
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
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<NoOpEmailSender>();
        services.AddScoped<IEmailSender>(sp =>
            configuration.GetValue("Notifications:Email:Enabled", false)
                ? sp.GetRequiredService<SmtpEmailSender>()
                : sp.GetRequiredService<NoOpEmailSender>());
        services.AddSingleton<IUserNotificationNotifier, NoOpUserNotificationNotifier>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<WebhookDeliveryService>();
        services.AddScoped<IWebhookEventPublisher, WebhookEventPublisher>();
        services.AddScoped<IWebhookDeliveryDispatcher, WebhookDeliveryDispatcher>();
        services.AddHttpClient(nameof(WebhookDeliveryService));
        services.AddScoped<EmailNotificationPublisher>();
        services.AddScoped<TeamsWebhookNotificationPublisher>();
        services.AddScoped<INotificationPublisher>(sp => new CompositeNotificationPublisher(
            [
                sp.GetRequiredService<EmailNotificationPublisher>(),
                sp.GetRequiredService<TeamsWebhookNotificationPublisher>()
            ],
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeNotificationPublisher>>()));
        services.AddScoped<IKnowledgeSearchService, KeywordKnowledgeSearchService>();
        services.AddScoped<GitRepositoryCloneService>();
        services.AddScoped<IGitRepositoryCloneService>(sp => sp.GetRequiredService<GitRepositoryCloneService>());
        services.AddScoped<RepositoryArchiveExtractService>();
        services.AddScoped<IRepositoryArchiveExtractService>(sp => sp.GetRequiredService<RepositoryArchiveExtractService>());
        services.AddScoped<GitHubAppCloneService>();
        services.AddScoped<IGitHubAppCloneService>(sp => sp.GetRequiredService<GitHubAppCloneService>());
        services.AddScoped<IGitHubAppRepositoryService, Services.Delivery.GitHubAppRepositoryService>();
        services.AddScoped<IDeploymentConfigBundleBuilder, Services.Delivery.DeploymentConfigBundleBuilder>();
        services.AddScoped<IDeploymentAdapter, Services.Delivery.GitHubActionsDeploymentAdapter>();
        services.AddScoped<IDeploymentAdapter, Services.Delivery.WebhookDeploymentAdapter>();
        services.AddScoped<IDeploymentAdapterFactory, Services.Delivery.DeploymentAdapterFactory>();
        services.AddScoped<ITestCaseCatalogService, Services.Delivery.TestCaseCatalogService>();
        services.AddScoped<ITestCaseRepoExporter, Services.Delivery.TestCaseRepoExporter>();
        services.AddScoped<Services.Delivery.SimulatedQaRunner>();
        services.AddScoped<Services.Delivery.PlaywrightQaRunner>();
        services.AddScoped<IQaRunner, Services.Delivery.QaRunnerSelector>();
        services.AddScoped<INightlyRegressionService, Services.Delivery.NightlyRegressionService>();
        services.AddScoped<IQaEvidenceService, Services.Delivery.QaEvidenceService>();
        services.AddScoped<IChangeWindowEvaluator, Services.Delivery.ChangeWindowEvaluator>();
        services.AddScoped<IDeliveryOrchestrationService, Services.Delivery.DeliveryOrchestrationService>();
        services.AddScoped<IDeliveryOrchestrationDispatcher, Services.Delivery.DeliveryOrchestrationDispatcher>();
        services.AddScoped<AttachmentScanService>();
        services.AddScoped<NoOpAttachmentScanService>();
        services.AddScoped<IAttachmentScanService>(sp =>
        {
            var enabled = configuration.GetValue("Attachments:Scanning:Enabled", true);
            return enabled
                ? sp.GetRequiredService<AttachmentScanService>()
                : sp.GetRequiredService<NoOpAttachmentScanService>();
        });
        services.AddScoped<IGitRepositoryScanner, RoslynRepositoryScanner>();
        services.AddScoped<EfEntityTableMapper>();
        services.AddScoped<SqlServerSchemaScanner>();
        services.AddScoped<PostgreSqlSchemaScanner>();
        services.AddScoped<DatabaseSchemaScannerFactory>();
        services.AddScoped<DatabaseSchemaIngestionService>();
        services.AddScoped<ISystemGraphBuilder, SystemGraphBuilderService>();
        services.AddScoped<ISchemaDriftDetector, SchemaDriftDetectorService>();
        services.AddScoped<ISystemIntelligenceFingerprintService, SystemIntelligenceFingerprintService>();
        services.AddScoped<IDocumentationExportService, DocumentationExportService>();
        services.AddScoped<IRefactorBlastRadiusService, RefactorBlastRadiusService>();
        services.AddScoped<IRefactorPlanGenerator, RefactorPlanGeneratorService>();
        services.AddScoped<IOnPremAgentService, OnPremAgentService>();
        services.AddScoped<ApplicationProfileGenerator>();
        services.AddScoped<IRepositoryIndexer, RepositoryIndexerService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddSingleton<PromptSanitizer>();
        services.AddSingleton<AiResponseValidator>();

        services.Configure<Options.AiOptions>(configuration.GetSection(Options.AiOptions.SectionName));
        services.Configure<Options.RetentionOptions>(configuration.GetSection(Options.RetentionOptions.SectionName));
        services.Configure<Application.Options.IndexingOptions>(configuration.GetSection(Application.Options.IndexingOptions.SectionName));
        services.Configure<Application.Options.SystemIntelligenceOptions>(
            configuration.GetSection(Application.Options.SystemIntelligenceOptions.SectionName));
        services.Configure<Application.Options.IntegrationsOptions>(
            configuration.GetSection(Application.Options.IntegrationsOptions.SectionName));
        services.AddScoped<IGitRepositoryHistoryService, GitRepositoryHistoryService>();
        services.AddScoped<IIndexFreshnessService, IndexFreshnessService>();
        services.AddScoped<IDataScalingStatusService, DataScalingStatusService>();
        services.AddScoped<IObservabilityStatusService, ObservabilityStatusService>();
        services.AddScoped<IOpenApiIngestionService, OpenApiIngestionService>();
        services.AddScoped<IPolyglotSymbolIngestionService, PolyglotSymbolIngestionService>();
        services.AddScoped<IChatIntakeService, ChatIntakeService>();
        services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();
        services.AddScoped<IServiceNowSyncService, ServiceNowSyncService>();
        services.Configure<Application.Options.CommercialOptions>(
            configuration.GetSection(Application.Options.CommercialOptions.SectionName));
        services.Configure<Application.Options.StripeOptions>(
            configuration.GetSection(Application.Options.StripeOptions.SectionName));
        services.Configure<Application.Options.TenantIsolationOptions>(
            configuration.GetSection(Application.Options.TenantIsolationOptions.SectionName));
        services.AddScoped<Application.Abstractions.ITenantSchemaAccessor, TenantSchemaAccessor>();
        services.AddScoped<Application.Abstractions.ITenantIsolationService, TenantIsolationService>();
        services.AddSingleton<TenantSearchPathConnectionInterceptor>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ITenantMeteringService, TenantMeteringService>();
        services.AddScoped<ITenantBillingService, TenantBillingService>();
        services.AddScoped<IStripeBillingService, StripeBillingService>();
        services.AddScoped<IApprovalPolicyEvaluator, ApprovalPolicyEvaluator>();
        services.AddScoped<ISlaEscalationService, SlaEscalationService>();
        services.AddSingleton<IAuditExportTokenService, AuditExportTokenService>();
        services.AddSingleton<IFeatureService, ConfigurationFeatureService>();
        services.AddSingleton<IRequestCollaborationNotifier, NoOpRequestCollaborationNotifier>();
        services.AddScoped<HangfireRepositoryIndexingDispatcher>();
        services.PostConfigure<Options.AiOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
            {
                options.OpenAI.ApiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.OpenAI.BaseUrl))
            {
                options.OpenAI.BaseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
            }

            var legacyModel = configuration["OpenAI:Model"];
            if (!string.IsNullOrWhiteSpace(legacyModel))
            {
                if (string.IsNullOrWhiteSpace(options.OpenAI.Models.EnhancementAnalysis))
                {
                    options.OpenAI.Models.EnhancementAnalysis = legacyModel;
                }

                if (string.IsNullOrWhiteSpace(options.OpenAI.Models.RefactorPlan))
                {
                    options.OpenAI.Models.RefactorPlan = legacyModel;
                }
            }
        });

        services.AddScoped<IPiiRedactionService, Services.Ai.PiiRedactionService>();
        services.AddScoped<IAiUsageBudgetService, Services.Ai.AiUsageBudgetService>();
        services.AddScoped<IChatCompletionService, Services.Ai.ChatCompletionService>();

        services.AddScoped<IAiAnalysisService>(sp =>
            new OpenAiAnalysisService(
                sp.GetRequiredService<IChatCompletionService>(),
                sp.GetRequiredService<PromptSanitizer>(),
                sp.GetRequiredService<AiResponseValidator>(),
                sp.GetRequiredService<IRiskScoringService>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiAnalysisService>>()));

        services.AddScoped<IIntakeCopilotService, IntakeCopilotService>();
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
        services.AddScoped<IPolicyUrlFetcher, PolicyUrlFetcher>();

        services.AddScoped<IBackgroundJobStatusService, BackgroundJobStatusService>();
        services.AddSingleton<IAuthenticationConfigurationService, AuthenticationConfigurationService>();
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddSingleton<ISoc2ReadinessService, Soc2ReadinessService>();
        services.AddSingleton<IPlatformRuntimeStatusService, PlatformRuntimeStatusService>();

        services.AddHttpClient(OpenAiHttpClientName)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(ExternalTicketsHttpClientName)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(QdrantHttpClientName, (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["VectorSearch:Qdrant:Url"] ?? "http://localhost:6333";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        });
        services.AddHttpClient(TeamsWebhookHttpClientName)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient(GitHubAppHttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
        });

        services.AddHttpClient(PolicyUrlFetcherHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
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

        services.AddScoped<IExternalTicketExporter, ServiceNowTicketExporter>(sp =>
            new ServiceNowTicketExporter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ExternalTicketsHttpClientName),
                configuration,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceNowTicketExporter>>()));

        services.AddScoped<IExternalTicketExporterFactory, ExternalTicketExporterFactory>();
        services.AddScoped<IDatabaseSchemaScanner, DatabaseSchemaScanner>();
        services.AddScoped<IDatabaseSchemaIngestionService, DatabaseSchemaIngestionService>();
        services.AddScoped<IEfEntityTableMapper, EfEntityTableMapper>();

        if (registerBackgroundJobs)
        {
            RegisterBackgroundJobs(services, configuration, defaultConnectionString, provider);
        }
        else if (registerHangfireMonitoring)
        {
            RegisterHangfireMonitoringStorage(services, configuration, defaultConnectionString, provider);
        }

        return services;
    }

    private static void RegisterHangfireMonitoringStorage(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString,
        DatabaseProvider provider)
    {
        if (!ShouldUseHangfire(configuration, provider))
        {
            return;
        }

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = configuration["BackgroundJobs:HangfireSchema"] ?? "hangfire"
                }));
    }

    private static bool ShouldUseHangfire(IConfiguration configuration, DatabaseProvider provider) =>
        (configuration["BackgroundJobs:Provider"] ?? "Polling")
            .Equals("Hangfire", StringComparison.OrdinalIgnoreCase)
        && provider == DatabaseProvider.PostgreSql;

    private static void RegisterBackgroundJobs(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString,
        DatabaseProvider provider)
    {
        services.AddScoped<RepositoryIndexingJobExecutor>();
        services.AddScoped<AiAnalysisJobExecutor>();
        services.AddScoped<ApplicationDiscoveryJobExecutor>();
        services.AddScoped<DatabaseSchemaScanJobExecutor>();
        services.AddScoped<ScheduledRepositoryRefreshJobExecutor>();
        services.AddScoped<DataRetentionJobExecutor>();
        services.AddScoped<SchemaDriftScanJobExecutor>();
        services.AddScoped<DriftDigestJobExecutor>();
        services.AddScoped<DeliveryOrchestrationJobExecutor>();
        services.AddScoped<WebhookDeliveryJobExecutor>();

        var jobProvider = configuration["BackgroundJobs:Provider"] ?? "Polling";
        var useHangfire = ShouldUseHangfire(configuration, provider);

        if (useHangfire)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    options => options.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = configuration["BackgroundJobs:HangfireSchema"] ?? "hangfire"
                    }));

            RegisterHangfireTelemetry(configuration);
            services.AddHangfireServer(options =>
            {
                options.Queues = ["default", "indexing"];
            });
            services.AddHostedService<HangfireRecurringJobInitializer>();
            return;
        }

        services.AddHostedService<RepositoryIndexingJob>();
        services.AddHostedService<AiAnalysisJob>();
        services.AddHostedService<ScheduledRepositoryRefreshJob>();
        services.AddHostedService<DatabaseSchemaScanJob>();
        services.AddHostedService<ApplicationDiscoveryJob>();
        services.AddHostedService<DataRetentionJob>();
        services.AddHostedService<DeliveryOrchestrationJob>();
    }

    private static void RegisterHangfireTelemetry(IConfiguration configuration)
    {
        var options = configuration
            .GetSection(Application.Options.ObservabilityOptions.SectionName)
            .Get<Application.Options.ObservabilityOptions>()
            ?? new Application.Options.ObservabilityOptions();

        OpenTelemetryServiceExtensions.RegisterHangfireTelemetryFilters(options);
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

    private static void ConfigureDbContextOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        DatabaseProvider provider,
        Application.Options.DatabaseScalingOptions scalingOptions)
    {
        if (provider == DatabaseProvider.PostgreSql)
        {
            var pooledConnectionString = ApplyPostgreSqlPoolSettings(connectionString, scalingOptions);
            options.UseNpgsql(pooledConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(Math.Clamp(scalingOptions.ConnectionTimeoutSeconds, 5, 300));
            });
        }
        else
        {
            options.UseSqlite(connectionString);
        }
    }

    private static string ApplyPostgreSqlPoolSettings(
        string connectionString,
        Application.Options.DatabaseScalingOptions scalingOptions)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = Math.Clamp(scalingOptions.MaxPoolSize, 1, 500),
            MinPoolSize = Math.Max(0, scalingOptions.MinPoolSize)
        };
        return builder.ConnectionString;
    }

    private static string ResolveConnectionString(IServiceProvider serviceProvider, string connectionString)
    {
        if (!connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var contentRoot = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;
        return SqliteConnectionStringResolver.Resolve(connectionString, contentRoot);
    }

    private enum DatabaseProvider
    {
        Sqlite,
        PostgreSql
    }
}
