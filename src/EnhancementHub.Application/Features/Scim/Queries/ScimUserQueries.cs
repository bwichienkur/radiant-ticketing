using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Scim.Queries;

public sealed record ListScimUsersQuery(int StartIndex = 1, int Count = 100)
    : IRequest<ScimListResponse<ScimUserResource>>;

public sealed record ScimListResponse<T>(
    IReadOnlyList<T> Resources,
    int TotalResults,
    int StartIndex,
    int ItemsPerPage);

public sealed record ScimUserResource(
    Guid Id,
    string ExternalId,
    string UserName,
    string DisplayName,
    bool Active,
    UserRole Role);

public sealed class ListScimUsersQueryHandler : IRequestHandler<ListScimUsersQuery, ScimListResponse<ScimUserResource>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListScimUsersQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<ScimListResponse<ScimUserResource>> Handle(
        ListScimUsersQuery request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(1, request.StartIndex);
        var count = Math.Clamp(request.Count, 1, 200);

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.ProvisionedViaScim || u.ExternalId != null);

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Email)
            .Skip(start - 1)
            .Take(count)
            .ToListAsync(cancellationToken);

        var resources = users.Select(u => new ScimUserResource(
            u.Id,
            u.ExternalId ?? u.Id.ToString(),
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.Role)).ToList();

        return new ScimListResponse<ScimUserResource>(resources, total, start, count);
    }
}

public sealed record GetScimUserByExternalIdQuery(string ExternalId) : IRequest<ScimUserResource?>;

public sealed class GetScimUserByExternalIdQueryHandler
    : IRequestHandler<GetScimUserByExternalIdQuery, ScimUserResource?>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetScimUserByExternalIdQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<ScimUserResource?> Handle(
        GetScimUserByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == request.ExternalId, cancellationToken);

        return user is null
            ? null
            : new ScimUserResource(
                user.Id,
                user.ExternalId!,
                user.Email,
                user.DisplayName,
                user.IsActive,
                user.Role);
    }
}
