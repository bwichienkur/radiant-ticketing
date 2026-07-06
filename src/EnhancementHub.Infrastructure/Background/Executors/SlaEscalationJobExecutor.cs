using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class SlaEscalationJobExecutor
{
    private readonly ISlaEscalationService _slaEscalationService;
    private readonly ILogger<SlaEscalationJobExecutor> _logger;

    public SlaEscalationJobExecutor(
        ISlaEscalationService slaEscalationService,
        ILogger<SlaEscalationJobExecutor> logger)
    {
        _slaEscalationService = slaEscalationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var count = await _slaEscalationService.ProcessEscalationsAsync(cancellationToken);
        if (count > 0)
        {
            _logger.LogInformation("SLA escalation job processed {Count} request(s).", count);
        }
    }
}
