using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class KeywordKnowledgeSearchService : IKnowledgeSearchService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public KeywordKnowledgeSearchService(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, string>? metadataFilters = null,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 1)
            .Distinct()
            .ToArray();

        if (terms.Length == 0)
        {
            return Array.Empty<KnowledgeSearchResult>();
        }

        var results = new List<KnowledgeSearchResult>();

        var indexedFiles = await _dbContext.IndexedFiles.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var file in indexedFiles)
        {
            if (!MatchesMetadataFilters(file, metadataFilters))
            {
                continue;
            }

            var haystack = $"{file.FilePath} {file.Namespace} {file.ClassName} {file.Summary}".ToLowerInvariant();
            var matched = terms.Count(t => haystack.Contains(t, StringComparison.Ordinal));
            if (matched == 0)
            {
                continue;
            }

            results.Add(new KnowledgeSearchResult(
                file.Id,
                file.ClassName ?? file.FilePath,
                (float)matched / terms.Length,
                BuildSnippet(file.Summary ?? file.FilePath, terms)));
        }

        var profiles = await _dbContext.ApplicationProfiles.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            var haystack = $"{profile.Purpose} {profile.KeyComponents} {profile.DatabaseUsage} {profile.ExternalIntegrations}".ToLowerInvariant();
            var matched = terms.Count(t => haystack.Contains(t, StringComparison.Ordinal));
            if (matched == 0)
            {
                continue;
            }

            results.Add(new KnowledgeSearchResult(
                profile.Id,
                profile.Purpose ?? "Application profile",
                (float)matched / terms.Length,
                BuildSnippet(profile.KeyComponents ?? profile.Purpose ?? string.Empty, terms)));
        }

        var settings = await _dbContext.SystemSettings.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var setting in settings)
        {
            if (metadataFilters is not null
                && metadataFilters.TryGetValue("category", out var category)
                && !string.Equals(setting.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var haystack = $"{setting.Key} {setting.Value} {setting.Description}".ToLowerInvariant();
            var matched = terms.Count(t => haystack.Contains(t, StringComparison.Ordinal));
            if (matched == 0)
            {
                continue;
            }

            results.Add(new KnowledgeSearchResult(
                setting.Id,
                setting.Key,
                (float)matched / terms.Length,
                BuildSnippet(setting.Value, terms)));
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    private static bool MatchesMetadataFilters(
        Domain.Entities.IndexedFile file,
        IReadOnlyDictionary<string, string>? filters)
    {
        if (filters is null || filters.Count == 0)
        {
            return true;
        }

        foreach (var (key, expected) in filters)
        {
            var actual = key.ToLowerInvariant() switch
            {
                "language" => file.Language,
                "componenttype" => file.ComponentType.ToString(),
                "namespace" => file.Namespace,
                "project" => file.Project,
                _ => null
            };

            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildSnippet(string content, string[] terms)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var index = content.Length;
        foreach (var term in terms)
        {
            var pos = content.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0 && pos < index)
            {
                index = pos;
            }
        }

        if (index == content.Length)
        {
            return content.Length <= 200 ? content : content[..200] + "...";
        }

        var start = Math.Max(0, index - 60);
        var length = Math.Min(200, content.Length - start);
        var snippet = content.Substring(start, length);
        return start > 0 ? "..." + snippet : snippet;
    }
}
