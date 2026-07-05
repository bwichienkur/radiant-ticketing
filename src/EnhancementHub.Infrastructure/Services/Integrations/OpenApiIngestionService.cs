using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class OpenApiIngestionService : IOpenApiIngestionService
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "options", "head", "trace"
    };

    private readonly IEnhancementHubDbContext _dbContext;

    public OpenApiIngestionService(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<OpenApiIngestionResult> IngestAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        var registration = await _dbContext.OpenApiRegistrations
            .Include(r => r.Endpoints)
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

        if (registration is null)
        {
            return new OpenApiIngestionResult(false, 0, "OpenAPI registration not found.");
        }

        var parsed = await ParseAndValidateAsync(registration.SpecDocument, cancellationToken);
        if (!parsed.Succeeded)
        {
            return parsed;
        }

        var endpoints = ParseEndpoints(registration.SpecDocument);
        _dbContext.OpenApiEndpoints.RemoveRange(registration.Endpoints);

        var now = DateTime.UtcNow;
        foreach (var endpoint in endpoints)
        {
            _dbContext.OpenApiEndpoints.Add(new Domain.Entities.OpenApiEndpoint
            {
                Id = Guid.NewGuid(),
                OpenApiRegistrationId = registration.Id,
                Path = endpoint.Path,
                HttpMethod = endpoint.HttpMethod,
                OperationId = endpoint.OperationId,
                Summary = endpoint.Summary,
                Tags = endpoint.Tags,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        registration.EndpointCount = endpoints.Count;
        registration.LastIngestedAt = now;
        registration.BaseUrl = ExtractBaseUrl(registration.SpecDocument);
        registration.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OpenApiIngestionResult(true, endpoints.Count, null);
    }

    public Task<OpenApiIngestionResult> ParseAndValidateAsync(
        string specDocument,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(specDocument))
        {
            return Task.FromResult(new OpenApiIngestionResult(false, 0, "Spec document is empty."));
        }

        try
        {
            using var doc = JsonDocument.Parse(specDocument);
            if (!doc.RootElement.TryGetProperty("paths", out var paths)
                || paths.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult(new OpenApiIngestionResult(
                    false,
                    0,
                    "OpenAPI document must contain a paths object."));
            }

            var count = ParseEndpoints(specDocument).Count;
            return Task.FromResult(new OpenApiIngestionResult(true, count, null));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new OpenApiIngestionResult(false, 0, ex.Message));
        }
    }

    internal static List<ParsedEndpoint> ParseEndpoints(string specDocument)
    {
        using var doc = JsonDocument.Parse(specDocument);
        var results = new List<ParsedEndpoint>();

        if (!doc.RootElement.TryGetProperty("paths", out var paths))
        {
            return results;
        }

        foreach (var pathEntry in paths.EnumerateObject())
        {
            foreach (var methodEntry in pathEntry.Value.EnumerateObject())
            {
                if (!HttpMethods.Contains(methodEntry.Name))
                {
                    continue;
                }

                var operation = methodEntry.Value;
                string? operationId = operation.TryGetProperty("operationId", out var opId)
                    ? opId.GetString()
                    : null;
                string? summary = operation.TryGetProperty("summary", out var sum)
                    ? sum.GetString()
                    : null;
                string? tags = operation.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", tagsEl.EnumerateArray().Select(t => t.GetString()).Where(t => t is not null))
                    : null;

                results.Add(new ParsedEndpoint(
                    pathEntry.Name,
                    methodEntry.Name.ToUpperInvariant(),
                    operationId,
                    summary,
                    tags));
            }
        }

        return results;
    }

    internal static string? ExtractBaseUrl(string specDocument)
    {
        using var doc = JsonDocument.Parse(specDocument);
        if (doc.RootElement.TryGetProperty("servers", out var servers)
            && servers.ValueKind == JsonValueKind.Array)
        {
            var first = servers.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object
                && first.TryGetProperty("url", out var url))
            {
                return url.GetString();
            }
        }

        return null;
    }

    internal sealed record ParsedEndpoint(
        string Path,
        string HttpMethod,
        string? OperationId,
        string? Summary,
        string? Tags);
}
