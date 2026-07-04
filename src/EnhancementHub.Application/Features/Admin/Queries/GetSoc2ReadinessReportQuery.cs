using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed record GetSoc2ReadinessReportQuery : IRequest<Soc2ReadinessReportDto>;
