using System.Security.Cryptography;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record CreateServiceApiKeyCommand(
    string Name,
    string? Description,
    UserRole Role,
    Guid? TeamId = null,
    int? ExpiresInDays = null) : IRequest<CreateServiceApiKeyResultDto>;

public sealed class CreateServiceApiKeyCommandValidator : AbstractValidator<CreateServiceApiKeyCommand>
{
    public CreateServiceApiKeyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0)
            .When(x => x.ExpiresInDays.HasValue);
    }
}

public sealed class CreateServiceApiKeyCommandHandler
    : IRequestHandler<CreateServiceApiKeyCommand, CreateServiceApiKeyResultDto>
{
    private const string KeyPrefix = "eh_";

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public CreateServiceApiKeyCommandHandler(
        IEnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<CreateServiceApiKeyResultDto> Handle(
        CreateServiceApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TeamId.HasValue)
        {
            var teamExists = await _dbContext.Teams
                .AnyAsync(t => t.Id == request.TeamId.Value, cancellationToken);
            if (!teamExists)
            {
                throw new ValidationException("Team not found.");
            }
        }

        var (plainKey, prefix) = GenerateApiKey();
        var now = DateTime.UtcNow;
        var serviceUserId = Guid.NewGuid();

        var serviceUser = new User
        {
            Id = serviceUserId,
            Email = $"service-{serviceUserId:N}@apikeys.internal",
            DisplayName = request.Name.Trim(),
            Role = request.Role,
            IsActive = true,
            PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
            CreatedAt = now,
            UpdatedAt = now
        };

        var apiKey = new ServiceApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            KeyPrefix = prefix,
            KeyHash = _passwordHasher.Hash(plainKey),
            ServiceUserId = serviceUserId,
            IsActive = true,
            ExpiresAt = request.ExpiresInDays.HasValue
                ? now.AddDays(request.ExpiresInDays.Value)
                : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(serviceUser);
        _dbContext.ServiceApiKeys.Add(apiKey);

        if (request.TeamId.HasValue)
        {
            _dbContext.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = request.TeamId.Value,
                UserId = serviceUserId,
                Role = TeamMemberRoles.Member,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "ServiceApiKeyCreated",
            nameof(ServiceApiKey),
            apiKey.Id,
            $"Created service API key '{apiKey.Name}' with role {request.Role}.",
            cancellationToken);

        return new CreateServiceApiKeyResultDto(
            apiKey.Id,
            apiKey.Name,
            plainKey,
            prefix,
            request.Role,
            apiKey.ExpiresAt);
    }

    private static (string PlainKey, string Prefix) GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var plainKey = KeyPrefix + secret;
        var prefix = plainKey[..11];
        return (plainKey, prefix);
    }
}
