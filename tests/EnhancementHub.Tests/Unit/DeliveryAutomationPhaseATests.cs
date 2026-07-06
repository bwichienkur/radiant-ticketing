using EnhancementHub.Application.Features.Delivery;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class DeliveryAutomationPhaseATests
{
    [Fact]
    public void DeliveryEntities_ExistInDomain()
    {
        var tenantProfile = new TenantDeliveryProfile { TenantId = Guid.NewGuid() };
        var environment = new TenantDeploymentEnvironment { TenantId = Guid.NewGuid(), Name = "Test" };
        var appProfile = new ApplicationDeliveryProfile { ApplicationId = Guid.NewGuid() };

        tenantProfile.DefaultCicdProvider.Should().Be(CicdProvider.GitHubActions);
        environment.EnvironmentType.Should().Be(DeploymentEnvironmentType.Test);
        appProfile.DeploymentMechanism.Should().Be(DeploymentMechanism.AppService);
    }

    [Fact]
    public void SpaDeliveryController_ExposesProfileEndpoints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaDeliveryController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("tenant-profile");
        content.Should().Contain("applications/{applicationId:guid}/profile");
        content.Should().Contain("ValidateApplicationDeliveryProfileQuery");
    }

    [Fact]
    public void AdminDeliveryPage_Exists()
    {
        var page = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Pages/Admin/Delivery.cshtml");
        var codeBehind = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Pages/Admin/Delivery.cshtml.cs");
        File.Exists(page).Should().BeTrue();
        File.ReadAllText(codeBehind).Should().Contain("UpdateTenantDeliveryProfileCommand");
    }

    [Fact]
    public void DeliveryProfileValidator_FlagsMissingPipelineReference()
    {
        var profile = new ApplicationDeliveryProfile
        {
            ApplicationId = Guid.NewGuid(),
            BranchNamingPattern = "eh/{requestId}",
            SmokeTestPath = "/health",
            CicdPipelineReference = null,
        };

        var result = DeliveryProfileValidator.ValidateApplicationProfile(profile);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("pipeline reference", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeliveryProfileValidator_WarnsWhenTestEnvironmentMissing()
    {
        var tenantProfile = new TenantDeliveryProfile { TenantId = Guid.NewGuid(), AutoDeployToTest = true };
        var result = DeliveryProfileValidator.ValidateTenantProfile(tenantProfile, []);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Test environment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeliveryAutomationRoadmap_DocumentsPhases()
    {
        var doc = File.ReadAllText(Path.Combine(GetRepoRoot(), "docs/DELIVERY_AUTOMATION_ROADMAP.md"));
        doc.Should().Contain("Phase A");
        doc.Should().Contain("Phase B");
        doc.Should().Contain("IDeploymentAdapter");
        doc.Should().Contain("EnhancementDeliveryRun");
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }
}
