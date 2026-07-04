using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class AzureSearchVectorService : IVectorSearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<AzureSearchVectorService> _logger;
    private readonly string _indexName;
    private readonly int _dimensions;
    private bool _indexEnsured;

    public AzureSearchVectorService(IConfiguration configuration, ILogger<AzureSearchVectorService> logger)
    {
        _logger = logger;
        var endpoint = configuration["VectorSearch:AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException("VectorSearch:AzureSearch:Endpoint is required when VectorSearch:Provider=AzureSearch.");
        var apiKey = configuration["VectorSearch:AzureSearch:ApiKey"]
            ?? throw new InvalidOperationException("VectorSearch:AzureSearch:ApiKey is required when VectorSearch:Provider=AzureSearch.");
        _indexName = configuration["VectorSearch:AzureSearch:IndexName"] ?? "indexed-files";
        _dimensions = configuration.GetValue("VectorSearch:Dimensions", 64);

        var credential = new AzureKeyCredential(apiKey);
        _indexClient = new SearchIndexClient(new Uri(endpoint), credential);
        _searchClient = _indexClient.GetSearchClient(_indexName);
    }

    public async Task IndexAsync(string sourceType, Guid sourceId, float[] embedding, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await EnsureIndexAsync(cancellationToken);

        var document = new SearchDocument
        {
            ["id"] = sourceId.ToString(),
            ["sourceType"] = sourceType,
            ["sourceId"] = sourceId.ToString(),
            ["embedding"] = VectorEmbeddingNormalizer.Normalize(embedding, _dimensions)
        };

        var batch = IndexDocumentsBatch.MergeOrUpload([document]);
        var response = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        if (!response.Value.Results.All(r => r.Succeeded))
        {
            var failures = string.Join(", ", response.Value.Results.Where(r => !r.Succeeded).Select(r => r.ErrorMessage));
            _logger.LogWarning("Azure Search index upsert had failures: {Failures}", failures);
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var options = new SearchOptions
        {
            Size = topK,
            Select = { "sourceId", "sourceType" },
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(VectorEmbeddingNormalizer.Normalize(queryEmbedding, _dimensions))
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "embedding" }
                    }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(null, options, cancellationToken);
        var results = new List<VectorSearchResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            if (!result.Document.TryGetValue("sourceId", out var sourceIdValue)
                || !Guid.TryParse(sourceIdValue?.ToString(), out var sourceId))
            {
                continue;
            }

            var sourceType = result.Document.TryGetValue("sourceType", out var sourceTypeValue)
                ? sourceTypeValue?.ToString() ?? "IndexedFile"
                : "IndexedFile";

            results.Add(new VectorSearchResult(sourceId, sourceType, (float)(result.Score ?? 0)));
        }

        return results;
    }

    public async Task RemoveBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await EnsureIndexAsync(cancellationToken);

        var batch = IndexDocumentsBatch.Delete("id", [sourceId.ToString()]);
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_indexEnsured)
        {
            return;
        }

        try
        {
            await _indexClient.GetIndexAsync(_indexName, cancellationToken);
            _indexEnsured = true;
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Creating Azure Search index {IndexName}", _indexName);
        }

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("sourceType") { IsFilterable = true },
                new SearchableField("sourceId") { IsFilterable = true },
                new VectorSearchField("embedding", _dimensions, "default-vector-profile")
            },
            VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile("default-vector-profile", "default-algorithm")
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("default-algorithm")
                }
            }
        };

        await _indexClient.CreateIndexAsync(index, cancellationToken);
        _indexEnsured = true;
    }
}
