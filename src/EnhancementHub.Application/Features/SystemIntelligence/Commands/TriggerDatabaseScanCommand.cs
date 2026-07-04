using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record TriggerDatabaseScanCommand(Guid ConnectionId) : IRequest<DatabaseConnectionDto>;

public sealed class TriggerDatabaseScanCommandHandler
    : IRequestHandler<TriggerDatabaseScanCommand, DatabaseConnectionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IDatabaseSchemaIngestionService _ingestionService;
    private readonly IApplicationAccessService _accessService;

    public TriggerDatabaseScanCommandHandler(
        IEnhancementHubDbContext dbContext,
        IDatabaseSchemaIngestionService ingestionService,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _ingestionService = ingestionService;
        _accessService = accessService;
    }

    public async Task<DatabaseConnectionDto> Handle(
        TriggerDatabaseScanCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleConnectionAsync(request.ConnectionId, cancellationToken);

        await _ingestionService.IngestAsync(request.ConnectionId, cancellationToken);

        var entity = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .Include(c => c.Application)
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException(nameof(DatabaseConnection), request.ConnectionId);

        return new DatabaseConnectionDto(
            entity.Id,
            entity.ApplicationId,
            entity.Application?.Name,
            entity.Name,
            entity.Provider,
            entity.IsReadOnly,
            entity.ScanStatus,
            entity.LastScannedAt,
            entity.ScanError);
    }
}
