using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IIntakeCopilotService
{
    Task<IntakeCopilotTurnResult> ProcessTurnAsync(
        IReadOnlyList<IntakeCopilotMessage> conversation,
        IntakeCopilotDraft? currentDraft,
        int turnCount,
        IntakeCopilotSource source,
        CancellationToken cancellationToken = default);
}
