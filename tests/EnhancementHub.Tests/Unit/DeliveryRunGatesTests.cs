using EnhancementHub.Application.Features.Delivery;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class DeliveryRunGatesTests
{
    [Fact]
    public void CanDeployToProduction_RequiresHumanProdDeployAndGates()
    {
        var run = new EnhancementDeliveryRun
        {
            Phase = DeliveryRunPhase.UatApproved,
            QaPassed = true,
            UatApproved = true,
        };
        var tenant = new TenantDeliveryProfile { AllowOneClickProdDeploy = true, RequireUatSignoff = true };
        var app = new ApplicationDeliveryProfile { RequiresHumanProdDeploy = true };

        DeliveryRunGates.CanDeployToProduction(run, tenant, app).Should().BeTrue();
    }

    [Fact]
    public void CanRollbackProduction_RequiresCompletedRunWithTarget()
    {
        var run = new EnhancementDeliveryRun
        {
            Phase = DeliveryRunPhase.Completed,
            RollbackTargetCommitSha = "abc123",
        };
        var tenant = new TenantDeliveryProfile { AllowOneClickRollback = true };

        DeliveryRunGates.CanRollbackProduction(run, tenant).Should().BeTrue();
    }
}
