using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;

namespace EnhancementHub.Application.Features.SystemIntelligence;

internal static class BlastRadiusMapper
{
    public static BlastRadiusResultDto ToDto(RefactorBlastRadiusResult result)
    {
        var items = new List<BlastRadiusItemDto>();
        foreach (var table in result.AffectedTables)
        {
            items.Add(new BlastRadiusItemDto(table, "Table", "Schema dependency", 0));
        }

        foreach (var entity in result.AffectedEntities)
        {
            items.Add(new BlastRadiusItemDto(entity, "Entity", "EF mapping", 1));
        }

        foreach (var service in result.AffectedServices)
        {
            items.Add(new BlastRadiusItemDto(service, "Service", "Data access", 2));
        }

        foreach (var api in result.AffectedApiEndpoints)
        {
            items.Add(new BlastRadiusItemDto(api, "API", "Consumer surface", 2));
        }

        return new BlastRadiusResultDto(result.TargetName, items);
    }
}
