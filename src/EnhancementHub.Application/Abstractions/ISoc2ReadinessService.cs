using EnhancementHub.Application.Features.Admin.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface ISoc2ReadinessService
{
    Soc2ReadinessReportDto GetReport();
}
