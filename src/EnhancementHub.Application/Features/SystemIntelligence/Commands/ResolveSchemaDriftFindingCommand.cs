using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record ResolveSchemaDriftFindingCommand(Guid FindingId) : IRequest<bool>;

public sealed class ResolveSchemaDriftFindingCommandHandler
    : IRequestHandler<ResolveSchemaDriftFindingCommand, bool>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ResolveSchemaDriftFindingCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(
        ResolveSchemaDriftFindingCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var finding = await _dbContext.SchemaDriftFindings
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.SchemaDriftFinding), request.FindingId);

        finding.IsResolved = true;
        finding.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
