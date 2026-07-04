using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase15EnterpriseHardeningTests
{
    [Fact]
    public void Pbkdf2PasswordHasher_VerifiesCorrectPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("password123");

        hasher.Verify("password123", hash).Should().BeTrue();
        hasher.Verify("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void ProductionConfigurationValidator_AllowsDevelopmentDefaults()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = ProductionConfigurationValidator.DevJwtSecret
        };

        var act = () => ProductionConfigurationValidator.Validate(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(configuration)
                .Build(),
            new TestHostEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void ProductionConfigurationValidator_RejectsDevJwtSecretInProduction()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = ProductionConfigurationValidator.DevJwtSecret,
            ["DataProtection:KeysPath"] = "/tmp/keys"
        };

        var act = () => ProductionConfigurationValidator.Validate(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(configuration)
                .Build(),
            new TestHostEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public async Task OnPremAgentService_RejectsScanWithoutValidApiKey()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        await factory.EnsureDatabaseInitializedAsync();

        var service = scope.ServiceProvider.GetRequiredService<IOnPremAgentService>();
        var registration = await service.RegisterAgentAsync("Test Agent", null);

        var act = async () => await service.AcceptScanPayloadAsync(
            registration.AgentId,
            "invalid-key",
            Guid.NewGuid(),
            new Application.Abstractions.Models.DatabaseSchemaScanResult(),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task OnPremAgentService_ValidApiKey_PassesAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var service = scope.ServiceProvider.GetRequiredService<IOnPremAgentService>();
        var registration = await service.RegisterAgentAsync("Test Agent", null);

        Exception? caught = null;
        try
        {
            await service.AcceptScanPayloadAsync(
                registration.AgentId,
                registration.ApiKey!,
                Guid.NewGuid(),
                new Application.Abstractions.Models.DatabaseSchemaScanResult(),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        caught.Should().NotBeOfType<UnauthorizedAccessException>();

        var agent = await db.OnPremAgents.SingleAsync(a => a.Id == registration.AgentId);
        agent.LastSeenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EnhancementRequestAccessService_FiltersRequestsBySubmitter()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await factory.EnsureDatabaseInitializedAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.local",
            DisplayName = "Owner",
            Role = UserRole.Submitter,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var other = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@test.local",
            DisplayName = "Other",
            Role = UserRole.Submitter,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(owner, other);
        db.EnhancementRequests.AddRange(
            new EnhancementRequest
            {
                Id = Guid.NewGuid(),
                Title = "Owner request",
                BusinessDescription = "A",
                DesiredOutcome = "B",
                Priority = "Low",
                SubmittedByUserId = owner.Id,
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EnhancementRequest
            {
                Id = Guid.NewGuid(),
                Title = "Other request",
                BusinessDescription = "A",
                DesiredOutcome = "B",
                Priority = "Low",
                SubmittedByUserId = other.Id,
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var accessService = new EnhancementRequestAccessService(
            db,
            new TestCurrentUserService(owner.Id, owner.Email, owner.DisplayName, UserRole.Submitter));

        var visible = await accessService.ApplyVisibilityFilter(db.EnhancementRequests).ToListAsync();
        visible.Should().ContainSingle(r => r.SubmittedByUserId == owner.Id);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(Guid userId, string email, string displayName, UserRole role)
        {
            UserId = userId;
            Email = email;
            DisplayName = displayName;
            Role = role;
            IsAuthenticated = true;
        }

        public Guid? UserId { get; }
        public string? Email { get; }
        public string? DisplayName { get; }
        public UserRole? Role { get; }
        public bool IsAuthenticated { get; }
        public string? IpAddress => "127.0.0.1";
    }

    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnhancementHub.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
