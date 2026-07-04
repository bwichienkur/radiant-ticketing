using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Queries;

public sealed record GetEnhancementRequestByIdQuery(Guid Id) : IRequest<EnhancementRequestDetailDto>;

public sealed class GetEnhancementRequestByIdQueryHandler
    : IRequestHandler<GetEnhancementRequestByIdQuery, EnhancementRequestDetailDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetEnhancementRequestByIdQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EnhancementRequestDetailDto> Handle(
        GetEnhancementRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Include(r => r.TargetApplication)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.Attachments)
            .Include(r => r.Comments).ThenInclude(c => c.User)
            .Include(r => r.Analyses)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.Id);

        return entity.ToDetailDto();
    }
}
