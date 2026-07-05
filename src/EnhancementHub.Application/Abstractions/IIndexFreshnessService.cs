using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IIndexFreshnessService
{
    Task<IndexFreshnessReportDto> GetReportAsync(CancellationToken cancellationToken = default);
}
