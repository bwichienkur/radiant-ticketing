using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Delivery;

public static class DeliveryRunGates
{
    public static bool CanDeployToProduction(
        EnhancementDeliveryRun run,
        TenantDeliveryProfile? tenantProfile,
        ApplicationDeliveryProfile? appProfile)
    {
        if (tenantProfile?.AllowOneClickProdDeploy == false)
        {
            return false;
        }

        if (run.QaPassed != true)
        {
            return false;
        }

        if (tenantProfile?.RequireUatSignoff == true && !run.UatApproved)
        {
            return false;
        }

        if (appProfile?.RequiresHumanProdDeploy != true)
        {
            return false;
        }

        return run.Phase is DeliveryRunPhase.UatApproved or DeliveryRunPhase.ProdScheduled;
    }

    public static bool CanRollbackProduction(
        EnhancementDeliveryRun run,
        TenantDeliveryProfile? tenantProfile)
    {
        if (tenantProfile?.AllowOneClickRollback == false)
        {
            return false;
        }

        if (run.Phase != DeliveryRunPhase.Completed)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(run.RollbackTargetDeployReference)
            || !string.IsNullOrWhiteSpace(run.RollbackTargetCommitSha);
    }
}
