using EnhancementHub.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DataRetentionJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RetentionOptions> _options;
    private readonly ILogger<DataRetentionJobExecutor> _logger;

    public DataRetentionJobExecutor(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionOptions> options,
        ILogger<DataRetentionJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var retentionService = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IDataRetentionService>();
        var result = await retentionService.ApplyAsync(dryRun: false, cancellationToken);

        if (result.AiPromptRunsDeleted > 0 || result.AttachmentsDeleted > 0)
        {
            _logger.LogInformation(
                "Scheduled data retention removed {AiPromptRuns} AI prompt runs and {Attachments} attachments",
                result.AiPromptRunsDeleted,
                result.AttachmentsDeleted);
        }
    }
}
