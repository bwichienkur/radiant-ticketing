using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;

namespace EnhancementHub.Application.Features.Analysis.Queries;

public sealed record GetEnhancementAnalysisQuery(
    Guid EnhancementRequestId,
    int? Version = null) : IRequest<EnhancementAnalysisDto>;

public sealed class GetEnhancementAnalysisQueryHandler
    : IRequestHandler<GetEnhancementAnalysisQuery, EnhancementAnalysisDto>
{
    private readonly IEnhancementAnalysisRepository _analyses;

    public GetEnhancementAnalysisQueryHandler(IEnhancementAnalysisRepository analyses) =>
        _analyses = analyses;

    public async Task<EnhancementAnalysisDto> Handle(
        GetEnhancementAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var analysis = await _analyses.GetByRequestAsync(
            request.EnhancementRequestId,
            request.Version,
            cancellationToken);

        if (analysis is null)
        {
            throw new NotFoundException(
                nameof(EnhancementAnalysis),
                $"request:{request.EnhancementRequestId}, version:{request.Version?.ToString() ?? "latest"}");
        }

        return analysis.ToDto();
    }
}
