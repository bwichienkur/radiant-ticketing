using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class QaRunnerSelector : IQaRunner
{
    private readonly IQaRunner _inner;

    public QaRunnerSelector(
        IConfiguration configuration,
        SimulatedQaRunner simulated,
        PlaywrightQaRunner playwright)
    {
        var runner = configuration.GetValue<string>("Delivery:Qa:Runner") ?? "Playwright";
        _inner = runner.Equals("Simulated", StringComparison.OrdinalIgnoreCase) ? simulated : playwright;
    }

    public QaRunnerKind RunnerKind => _inner.RunnerKind;

    public Task<QaEvidenceResult> RunAsync(QaRunManifest manifest, CancellationToken cancellationToken = default) =>
        _inner.RunAsync(manifest, cancellationToken);
}
