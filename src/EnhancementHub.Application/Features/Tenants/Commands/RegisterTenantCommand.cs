using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Tenants.Dtos;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Application.Features.Tenants.Commands;

public sealed record RegisterTenantCommand(
    string OrganizationName,
    string Slug,
    string AdminEmail,
    string AdminPassword,
    string AdminDisplayName,
    TenantRegion Region) : IRequest<RegisterTenantResultDto>;

public sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).MinimumLength(8);
        RuleFor(x => x.AdminDisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, RegisterTenantResultDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly CommercialOptions _options;
    private readonly ITenantIsolationService _tenantIsolationService;

    public RegisterTenantCommandHandler(
        IEnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<CommercialOptions> options,
        ITenantIsolationService tenantIsolationService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _options = options.Value;
        _tenantIsolationService = tenantIsolationService;
    }

    public async Task<RegisterTenantResultDto> Handle(
        RegisterTenantCommand request,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.SelfServiceSignupEnabled)
        {
            throw new ForbiddenException("Self-service signup is disabled.");
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        var email = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _dbContext.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Slug", "Slug is already in use.") });
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("AdminEmail", "Email is already registered.") });
        }

        var now = DateTime.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName.Trim(),
            Slug = slug,
            Plan = TenantPlan.Trial,
            Region = request.Region,
            TrialEndsAt = now.AddDays(_options.TrialDays),
            BillingEmail = email,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = $"{tenant.Name} Team",
            Description = "Default team for trial sandbox",
            TenantId = tenant.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.AdminDisplayName.Trim(),
            Role = UserRole.Admin,
            TenantId = tenant.Id,
            IsActive = true,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Teams.Add(team);
        _dbContext.Users.Add(admin);
        _dbContext.TeamMembers.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = admin.Id,
            Role = "Owner",
            CreatedAt = now,
            UpdatedAt = now
        });

        _dbContext.TenantUsageSnapshots.Add(new TenantUsageSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PeriodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            ApplicationCount = 0,
            AnalysisCount = 0,
            StorageBytes = 0,
            CapturedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _tenantIsolationService.TryAutoProvisionAsync(tenant.Id, cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(admin);
        return new RegisterTenantResultDto(
            tenant.Id,
            tenant.Slug,
            tenant.Plan.ToString(),
            tenant.Region.ToString(),
            tenant.TrialEndsAt,
            token,
            admin.Id,
            admin.Email);
    }
}
