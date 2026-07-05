using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class ListServiceApiKeysQueryHandler
    : IRequestHandler<ListServiceApiKeysQuery, IReadOnlyList<ServiceApiKeySummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListServiceApiKeysQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ServiceApiKeySummaryDto>> Handle(
        ListServiceApiKeysQuery request,
        CancellationToken cancellationToken) =>
        await _dbContext.ServiceApiKeys
            .AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ServiceApiKeySummaryDto(
                k.Id,
                k.Name,
                k.Description,
                k.KeyPrefix,
                k.ServiceUser.Role,
                k.IsActive,
                k.ExpiresAt,
                k.LastUsedAt,
                k.CreatedAt))
            .ToListAsync(cancellationToken);
}
