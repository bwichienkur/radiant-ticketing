using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class OpenApiRegistration : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpecDocument { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public int EndpointCount { get; set; }
    public DateTime? LastIngestedAt { get; set; }

    public Application Application { get; set; } = null!;
    public ICollection<OpenApiEndpoint> Endpoints { get; set; } = new List<OpenApiEndpoint>();
}
