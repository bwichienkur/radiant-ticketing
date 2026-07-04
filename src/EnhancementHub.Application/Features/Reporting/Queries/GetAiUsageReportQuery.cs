using EnhancementHub.Application.Features.Reporting.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed record GetAiUsageReportQuery : IRequest<AiUsageReportDto>;
