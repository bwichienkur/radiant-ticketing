using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record RevokeServiceApiKeyCommand(Guid ApiKeyId) : IRequest<bool>;

public sealed class RevokeServiceApiKeyCommandValidator : AbstractValidator<RevokeServiceApiKeyCommand>
{
    public RevokeServiceApiKeyCommandValidator() =>
        RuleFor(x => x.ApiKeyId).NotEmpty();
}

public sealed class RevokeServiceApiKeyCommandHandler : IRequestHandler<RevokeServiceApiKeyCommand, bool>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RevokeServiceApiKeyCommandHandler(IEnhancementHubDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<bool> Handle(RevokeServiceApiKeyCommand request, CancellationToken cancellationToken)
    {
        var apiKey = await _dbContext.ServiceApiKeys
            .Include(k => k.ServiceUser)
            .FirstOrDefaultAsync(k => k.Id == request.ApiKeyId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceApiKey), request.ApiKeyId);

        if (!apiKey.IsActive)
        {
            return true;
        }

        apiKey.IsActive = false;
        apiKey.UpdatedAt = DateTime.UtcNow;
        apiKey.ServiceUser.IsActive = false;
        apiKey.ServiceUser.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ServiceApiKeyRevoked",
            nameof(ServiceApiKey),
            apiKey.Id,
            $"Revoked service API key '{apiKey.Name}'.",
            cancellationToken);

        return true;
    }
}
