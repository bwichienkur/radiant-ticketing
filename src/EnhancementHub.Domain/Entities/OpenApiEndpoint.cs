using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class OpenApiEndpoint : BaseEntity
{
    public Guid OpenApiRegistrationId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? OperationId { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }

    public OpenApiRegistration Registration { get; set; } = null!;
}
