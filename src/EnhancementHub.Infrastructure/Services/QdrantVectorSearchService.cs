using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class QdrantVectorSearchService : IVectorSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QdrantVectorSearchService> _logger;
    private readonly string _collection;
    private readonly int _dimensions;

    public QdrantVectorSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantVectorSearchService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(InfrastructureServiceExtensions.QdrantHttpClientName);
        _logger = logger;
        _collection = configuration["VectorSearch:Qdrant:Collection"] ?? "indexed_files";
        _dimensions = configuration.GetValue("VectorSearch:Dimensions", 64);

        var baseUrl = configuration["VectorSearch:Qdrant:Url"] ?? "http://localhost:6333";
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task IndexAsync(string sourceType, Guid sourceId, float[] embedding, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await EnsureCollectionAsync(cancellationToken);

        var payload = new UpsertPointsRequest
        {
            Points =
            [
                new QdrantPoint
                {
                    Id = sourceId,
                    Vector = VectorEmbeddingNormalizer.Normalize(embedding, _dimensions),
                    Payload = new Dictionary<string, object>
                    {
                        ["sourceType"] = sourceType,
                        ["sourceId"] = sourceId.ToString()
                    }
                }
            ]
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"collections/{_collection}/points?wait=true",
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Qdrant upsert failed: {Status} {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(cancellationToken);

        var request = new SearchPointsRequest
        {
            Vector = VectorEmbeddingNormalizer.Normalize(queryEmbedding, _dimensions),
            Limit = topK,
            WithPayload = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"collections/{_collection}/points/search",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Qdrant search failed: {Status} {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var searchResponse = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: cancellationToken);
        if (searchResponse?.Result is null)
        {
            return [];
        }

        return searchResponse.Result
            .Select(point => new VectorSearchResult(
                point.Id,
                GetPayloadString(point.Payload, "sourceType") ?? "IndexedFile",
                point.Score))
            .ToList();
    }

    private static string? GetPayloadString(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is null || !payload.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    public async Task RemoveBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var request = new DeletePointsRequest
        {
            Points = [sourceId]
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"collections/{_collection}/points/delete?wait=true",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Qdrant delete failed: {Status} {Body}", response.StatusCode, body);
        }
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        var getResponse = await _httpClient.GetAsync($"collections/{_collection}", cancellationToken);
        if (getResponse.IsSuccessStatusCode)
        {
            return;
        }

        var createRequest = new CreateCollectionRequest
        {
            Vectors = new VectorParams
            {
                Size = _dimensions,
                Distance = "Cosine"
            }
        };

        var createResponse = await _httpClient.PutAsJsonAsync(
            $"collections/{_collection}",
            createRequest,
            cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
        {
            var body = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Qdrant collection create failed: {Status} {Body}", createResponse.StatusCode, body);
            createResponse.EnsureSuccessStatusCode();
        }
    }

    private sealed class CreateCollectionRequest
    {
        [JsonPropertyName("vectors")]
        public VectorParams Vectors { get; set; } = new();
    }

    private sealed class VectorParams
    {
        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("distance")]
        public string Distance { get; set; } = "Cosine";
    }

    private sealed class UpsertPointsRequest
    {
        [JsonPropertyName("points")]
        public List<QdrantPoint> Points { get; set; } = [];
    }

    private sealed class QdrantPoint
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("vector")]
        public float[] Vector { get; set; } = [];

        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }
    }

    private sealed class SearchPointsRequest
    {
        [JsonPropertyName("vector")]
        public float[] Vector { get; set; } = [];

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("with_payload")]
        public bool WithPayload { get; set; }
    }

    private sealed class QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public List<QdrantScoredPoint>? Result { get; set; }
    }

    private sealed class QdrantScoredPoint
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, JsonElement>? Payload { get; set; }
    }

    private sealed class DeletePointsRequest
    {
        [JsonPropertyName("points")]
        public List<Guid> Points { get; set; } = [];
    }
}
