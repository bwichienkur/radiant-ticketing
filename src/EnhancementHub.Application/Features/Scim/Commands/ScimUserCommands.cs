using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Scim.Commands;

public sealed record ProvisionScimUserCommand(
    string ExternalId,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive) : IRequest<ScimUserResult>;

public sealed record ScimUserResult(
    Guid Id,
    string ExternalId,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    bool Created);

public sealed class ProvisionScimUserCommandHandler : IRequestHandler<ProvisionScimUserCommand, ScimUserResult>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ProvisionScimUserCommandHandler(
        IEnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<ScimUserResult> Handle(
        ProvisionScimUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var externalId = request.ExternalId.Trim();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.ExternalId == externalId || u.Email.ToLower() == email,
                cancellationToken);

        var created = false;
        var now = DateTime.UtcNow;

        if (user is null)
        {
            created = true;
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = request.DisplayName.Trim(),
                Role = request.Role,
                IsActive = request.IsActive,
                ExternalId = externalId,
                ProvisionedViaScim = true,
                PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.DisplayName = request.DisplayName.Trim();
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.ExternalId = externalId;
            user.ProvisionedViaScim = true;
            user.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ScimUserResult(
            user.Id,
            user.ExternalId!,
            user.Email,
            user.DisplayName,
            user.Role,
            user.IsActive,
            created);
    }
}

public sealed record DeactivateScimUserCommand(string ExternalId) : IRequest<bool>;

public sealed class DeactivateScimUserCommandHandler : IRequestHandler<DeactivateScimUserCommand, bool>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public DeactivateScimUserCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<bool> Handle(DeactivateScimUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.ExternalId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
