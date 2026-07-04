using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record RegisterDatabaseConnectionCommand(
    Guid ApplicationId,
    string Name,
    DatabaseProviderType Provider,
    string ConnectionString,
    bool IsReadOnly) : IRequest<DatabaseConnectionDto>;

public sealed class RegisterDatabaseConnectionCommandValidator : AbstractValidator<RegisterDatabaseConnectionCommand>
{
    public RegisterDatabaseConnectionCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConnectionString).NotEmpty().MaximumLength(4000);
    }
}

public sealed class RegisterDatabaseConnectionCommandHandler
    : IRequestHandler<RegisterDatabaseConnectionCommand, DatabaseConnectionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IConnectionStringProtector _connectionStringProtector;
    private readonly IApplicationAccessService _accessService;
    private readonly IAuditService _auditService;

    public RegisterDatabaseConnectionCommandHandler(
        IEnhancementHubDbContext dbContext,
        IConnectionStringProtector connectionStringProtector,
        IApplicationAccessService accessService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _connectionStringProtector = connectionStringProtector;
        _accessService = accessService;
        _auditService = auditService;
    }

    public async Task<DatabaseConnectionDto> Handle(
        RegisterDatabaseConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _accessService.GetAccessibleApplicationAsync(
            request.ApplicationId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new DatabaseConnection
        {
            Id = Guid.NewGuid(),
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            Provider = request.Provider,
            ConnectionStringProtected = _connectionStringProtector.Protect(request.ConnectionString),
            IsReadOnly = request.IsReadOnly,
            ScanStatus = nameof(SchemaScanStatus.Pending),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.DatabaseConnections.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "DatabaseConnectionRegistered",
            nameof(DatabaseConnection),
            entity.Id,
            $"Registered database connection '{entity.Name}'",
            cancellationToken);

        return new DatabaseConnectionDto(
            entity.Id,
            entity.ApplicationId,
            application.Name,
            entity.Name,
            entity.Provider,
            entity.IsReadOnly,
            entity.ScanStatus,
            entity.LastScannedAt,
            entity.ScanError);
    }
}
