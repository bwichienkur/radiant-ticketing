using System.Security.Cryptography;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record CreateWebhookSubscriptionCommand(
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    Guid? TenantId = null) : IRequest<CreateWebhookSubscriptionResultDto>;

public sealed class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000).Must(BeValidHttpsUrl)
            .WithMessage("Webhook URL must be a valid http or https URL.");
        RuleFor(x => x.EventTypes).NotEmpty();
        RuleForEach(x => x.EventTypes)
            .Must(type => WebhookEventTypes.All.Any(known => string.Equals(known, type, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Unsupported webhook event type.");
    }

    private static bool BeValidHttpsUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

public sealed class CreateWebhookSubscriptionCommandHandler
    : IRequestHandler<CreateWebhookSubscriptionCommand, CreateWebhookSubscriptionResultDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IConnectionStringProtector _secretProtector;
    private readonly IAuditService _auditService;

    public CreateWebhookSubscriptionCommandHandler(
        IEnhancementHubDbContext dbContext,
        IConnectionStringProtector secretProtector,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
        _auditService = auditService;
    }

    public async Task<CreateWebhookSubscriptionResultDto> Handle(
        CreateWebhookSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var (plainSecret, prefix) = GenerateWebhookSecret();
        var now = DateTime.UtcNow;
        var eventTypes = string.Join(',', request.EventTypes.Distinct(StringComparer.OrdinalIgnoreCase));

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            SecretPrefix = prefix,
            SecretProtected = _secretProtector.Protect(plainSecret),
            EventTypes = eventTypes,
            TenantId = request.TenantId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "WebhookSubscriptionCreated",
            nameof(WebhookSubscription),
            subscription.Id,
            $"Created webhook subscription '{subscription.Name}' for events: {eventTypes}.",
            cancellationToken);

        return new CreateWebhookSubscriptionResultDto(
            subscription.Id,
            subscription.Name,
            plainSecret,
            prefix,
            eventTypes);
    }

    private static (string PlainSecret, string Prefix) GenerateWebhookSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var plainSecret = $"whsec_{secret}";
        return (plainSecret, plainSecret[..12]);
    }
}

public sealed record RevokeWebhookSubscriptionCommand(Guid SubscriptionId) : IRequest<Unit>;

public sealed class RevokeWebhookSubscriptionCommandHandler
    : IRequestHandler<RevokeWebhookSubscriptionCommand, Unit>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RevokeWebhookSubscriptionCommandHandler(
        IEnhancementHubDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<Unit> Handle(RevokeWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new ValidationException("Webhook subscription not found.");

        subscription.IsActive = false;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "WebhookSubscriptionRevoked",
            nameof(WebhookSubscription),
            subscription.Id,
            $"Revoked webhook subscription '{subscription.Name}'.",
            cancellationToken);

        return Unit.Value;
    }
}
