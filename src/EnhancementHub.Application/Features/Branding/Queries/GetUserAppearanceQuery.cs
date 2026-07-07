using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Branding.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Branding.Queries;

public sealed record GetUserAppearanceQuery : IRequest<UserAppearanceDto>;

public sealed class GetUserAppearanceQueryHandler : IRequestHandler<GetUserAppearanceQuery, UserAppearanceDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetUserAppearanceQueryHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<UserAppearanceDto> Handle(GetUserAppearanceQuery request, CancellationToken cancellationToken)
    {
        var theme = ThemePreference.System;
        TenantBrandingDto branding = new(null, "#2563eb", null);

        if (_currentUser.UserId is Guid userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is not null)
            {
                theme = user.ThemePreference;
                if (user.TenantId is Guid tenantId)
                {
                    branding = await LoadBrandingAsync(tenantId, cancellationToken);
                }
            }
        }

        return new UserAppearanceDto(theme.ToString(), branding);
    }

    internal static async Task<TenantBrandingDto> LoadBrandingAsync(
        IEnhancementHubDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var branding = await dbContext.TenantBrandings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

        return branding is null
            ? new TenantBrandingDto(null, "#2563eb", null)
            : new TenantBrandingDto(branding.LogoUrl, branding.AccentColor, branding.ProductName);
    }

    private Task<TenantBrandingDto> LoadBrandingAsync(Guid tenantId, CancellationToken cancellationToken) =>
        LoadBrandingAsync(_dbContext, tenantId, cancellationToken);
}
