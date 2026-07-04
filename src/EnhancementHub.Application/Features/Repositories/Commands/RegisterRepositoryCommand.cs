using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.Repositories.Commands;

public sealed record RegisterRepositoryCommand(
    Guid ApplicationId,
    string Name,
    string Url,
    ExternalTicketProvider Provider,
    string DefaultBranch = "main",
    string? GitTokenSecretName = null) : IRequest<RepositoryDto>;

public sealed class RegisterRepositoryCommandHandler
    : IRequestHandler<RegisterRepositoryCommand, RepositoryDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RegisterRepositoryCommandHandler(
        IEnhancementHubDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<RepositoryDto> Handle(
        RegisterRepositoryCommand request,
        CancellationToken cancellationToken)
    {
        var applicationExists = await _dbContext.Applications
            .AnyAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (!applicationExists)
        {
            throw new NotFoundException(nameof(ApplicationEntity), request.ApplicationId);
        }

        var entity = new Repository
        {
            Id = Guid.NewGuid(),
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            Url = request.Url,
            Provider = request.Provider,
            DefaultBranch = request.DefaultBranch,
            GitTokenSecretName = request.GitTokenSecretName,
            IndexingStatus = IndexingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Repositories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "RepositoryRegistered",
            nameof(Repository),
            entity.Id,
            $"Registered repository '{entity.Name}' for application {request.ApplicationId}",
            cancellationToken);

        var application = await _dbContext.Applications
            .AsNoTracking()
            .FirstAsync(a => a.Id == request.ApplicationId, cancellationToken);

        entity.Application = application;
        return entity.ToDto();
    }
}
