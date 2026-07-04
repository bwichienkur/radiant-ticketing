using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IRefactorPlanGenerator
{
    Task<RefactorPlanResult> GenerateAsync(
        string targetDescription,
        Guid? enhancementRequestId,
        Guid? databaseConnectionId,
        Guid? repositoryId,
        RefactorBlastRadiusResult? blastRadius,
        CancellationToken cancellationToken = default);
}
