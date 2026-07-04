using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ExternalTicketExporterFactory : IExternalTicketExporterFactory
{
    private readonly IEnumerable<IExternalTicketExporter> _exporters;

    public ExternalTicketExporterFactory(IEnumerable<IExternalTicketExporter> exporters)
    {
        _exporters = exporters;
    }

    public IExternalTicketExporter GetExporter(ExternalTicketProvider provider)
    {
        return _exporters.FirstOrDefault(e => e.Provider == provider)
            ?? throw new InvalidOperationException($"No ticket exporter registered for provider '{provider}'.");
    }
}
