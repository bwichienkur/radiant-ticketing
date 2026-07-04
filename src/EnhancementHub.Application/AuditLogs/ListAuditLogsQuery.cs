using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.AuditLogs;

public sealed record ListAuditLogsQuery(
    string? EntityType = null,
    string? Action = null,
    Guid? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Limit = 100) : IRequest<IReadOnlyList<AuditLogDto>>;

public sealed class ListAuditLogsQueryHandler : IRequestHandler<ListAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    private readonly IEnhancementHubDbContext _db;

    public ListAuditLogsQueryHandler(IEnhancementHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditLogDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId);
        if (request.From.HasValue)
            query = query.Where(a => a.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(a => a.CreatedAt <= request.To.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(request.Limit)
            .Select(a => new AuditLogDto(
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.User != null ? a.User.DisplayName : null,
                a.Comments,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
