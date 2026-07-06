using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IPlatformRuntimeStatusService
{
    PlatformRuntimeStatus GetStatus();
}
