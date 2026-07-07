using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Branding.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Branding.Commands;

public sealed record UpdateThemePreferenceCommand(string ThemePreference) : IRequest<UserAppearanceDto>;

public sealed class UpdateThemePreferenceCommandValidator : AbstractValidator<UpdateThemePreferenceCommand>
{
    public UpdateThemePreferenceCommandValidator()
    {
        RuleFor(x => x.ThemePreference).NotEmpty();
    }
}

public sealed class UpdateThemePreferenceCommandHandler
    : IRequestHandler<UpdateThemePreferenceCommand, UserAppearanceDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public UpdateThemePreferenceCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<UserAppearanceDto> Handle(
        UpdateThemePreferenceCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        if (!Enum.TryParse<ThemePreference>(request.ThemePreference, true, out var preference))
        {
            throw new ValidationException("Theme preference must be System, Light, or Dark.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.ThemePreference = preference;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var branding = user.TenantId is Guid tenantId
            ? await Queries.GetUserAppearanceQueryHandler.LoadBrandingAsync(_dbContext, tenantId, cancellationToken)
            : new TenantBrandingDto(null, "#2563eb", null);

        return new UserAppearanceDto(preference.ToString(), branding);
    }
}

public sealed record UpdateTenantBrandingCommand(
    string? LogoUrl,
    string AccentColor,
    string? ProductName) : IRequest<TenantBrandingDto>;

public sealed class UpdateTenantBrandingCommandValidator : AbstractValidator<UpdateTenantBrandingCommand>
{
    public UpdateTenantBrandingCommandValidator()
    {
        RuleFor(x => x.AccentColor).NotEmpty().MaximumLength(16);
        RuleFor(x => x.LogoUrl).MaximumLength(2048);
        RuleFor(x => x.ProductName).MaximumLength(120);
    }
}

public sealed class UpdateTenantBrandingCommandHandler
    : IRequestHandler<UpdateTenantBrandingCommand, TenantBrandingDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public UpdateTenantBrandingCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<TenantBrandingDto> Handle(
        UpdateTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.TenantId is not Guid tenantId)
        {
            throw new InvalidOperationException("Tenant context is required to update branding.");
        }

        var branding = await _dbContext.TenantBrandings
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

        var now = DateTime.UtcNow;
        if (branding is null)
        {
            branding = new TenantBranding
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            _dbContext.TenantBrandings.Add(branding);
        }

        branding.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        branding.AccentColor = request.AccentColor.Trim();
        branding.ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? null : request.ProductName.Trim();
        branding.UpdatedAt = now;
        branding.UpdatedBy = userId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantBrandingDto(branding.LogoUrl, branding.AccentColor, branding.ProductName);
    }
}
