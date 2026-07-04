using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.ExternalTickets.Commands;

public sealed record ExternalTicketExportDto(
    Guid Id,
    ExternalTicketProvider Provider,
    string ExternalId,
    string ExternalUrl,
    string? Summary);

public sealed record ExportExternalTicketCommand(
    Guid EnhancementRequestId,
    ExternalTicketProvider Provider) : IRequest<ExternalTicketExportDto>;

public sealed class ExportExternalTicketCommandHandler
    : IRequestHandler<ExportExternalTicketCommand, ExternalTicketExportDto>
{
    private static readonly EnhancementRequestStatus[] AllowedStatuses =
    [
        EnhancementRequestStatus.Approved,
        EnhancementRequestStatus.ReadyForDevelopment
    ];

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IExternalTicketExporterFactory _exporterFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ExportExternalTicketCommandHandler(
        IEnhancementHubDbContext dbContext,
        IExternalTicketExporterFactory exporterFactory,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _exporterFactory = exporterFactory;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<ExternalTicketExportDto> Handle(
        ExportExternalTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User must be authenticated to export external tickets.");
        }

        var enhancementRequest = await _dbContext.EnhancementRequests
            .Include(r => r.Analyses)
            .FirstOrDefaultAsync(r => r.Id == request.EnhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.EnhancementRequestId);

        if (!AllowedStatuses.Contains(enhancementRequest.Status))
        {
            throw new ForbiddenException(
                $"External tickets can only be exported when status is Approved or ReadyForDevelopment. Current status: {enhancementRequest.Status}.");
        }

        var latestAnalysis = enhancementRequest.Analyses
            .OrderByDescending(a => a.Version)
            .FirstOrDefault();

        var exportRequest = new ExternalTicketExportRequest(
            enhancementRequest.Id,
            enhancementRequest.Title,
            $"{enhancementRequest.BusinessDescription}\n\nDesired outcome: {enhancementRequest.DesiredOutcome}",
            enhancementRequest.Priority,
            latestAnalysis?.FeatureSummary);

        var exporter = _exporterFactory.GetExporter(request.Provider);
        var result = await exporter.ExportAsync(exportRequest, cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.ExternalId))
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to export external ticket.");
        }

        var ticket = new ExternalTicket
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = enhancementRequest.Id,
            Provider = request.Provider,
            ExternalId = result.ExternalId,
            ExternalUrl = result.Url ?? string.Empty,
            Summary = latestAnalysis?.FeatureSummary,
            CreatedByUserId = _currentUser.UserId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ExternalTickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ExternalTicketExported",
            nameof(ExternalTicket),
            ticket.Id,
            $"Exported to {request.Provider}: {result.ExternalId}",
            cancellationToken);

        return new ExternalTicketExportDto(
            ticket.Id,
            ticket.Provider,
            ticket.ExternalId,
            ticket.ExternalUrl,
            ticket.Summary);
    }
}
