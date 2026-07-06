using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Queries;

public sealed record GetDeliveryRunQuery(Guid EnhancementRequestId) : IRequest<EnhancementDeliveryRunDto?>;

public sealed class GetDeliveryRunQueryHandler : IRequestHandler<GetDeliveryRunQuery, EnhancementDeliveryRunDto?>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly IFileStorageService _fileStorage;

    public GetDeliveryRunQueryHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService,
        IFileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _fileStorage = fileStorage;
    }

    public async Task<EnhancementDeliveryRunDto?> Handle(
        GetDeliveryRunQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.GetAccessibleRequestAsync(request.EnhancementRequestId, cancellationToken);

        var run = await _dbContext.EnhancementDeliveryRuns
            .AsNoTracking()
            .Include(r => r.TestResults)
            .Where(r => r.EnhancementRequestId == request.EnhancementRequestId)
            .OrderByDescending(r => r.RunNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return run is null ? null : await DeliveryRunMapper.ToDtoAsync(run, _fileStorage, cancellationToken);
    }
}
