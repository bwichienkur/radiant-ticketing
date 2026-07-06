using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class TestCaseRepoExporter : ITestCaseRepoExporter
{
    private readonly ITestCaseCatalogService _catalog;
    private readonly IGitHubAppRepositoryService _gitHub;
    private readonly ILogger<TestCaseRepoExporter> _logger;

    public TestCaseRepoExporter(
        ITestCaseCatalogService catalog,
        IGitHubAppRepositoryService gitHub,
        ILogger<TestCaseRepoExporter> logger)
    {
        _catalog = catalog;
        _gitHub = gitHub;
        _logger = logger;
    }

    public async Task ExportRequestCasesToBranchAsync(
        Guid enhancementRequestId,
        string owner,
        string repository,
        string branch,
        CancellationToken cancellationToken = default)
    {
        await _catalog.EnsureDraftCasesForRequestAsync(enhancementRequestId, cancellationToken);
        var cases = await _catalog.GetExportableCasesForRequestAsync(enhancementRequestId, cancellationToken);
        if (cases.Count == 0)
        {
            _logger.LogInformation("No exportable test cases for request {RequestId}", enhancementRequestId);
            return;
        }

        var spec = PlaywrightSpecGenerator.GenerateCombinedSpec(enhancementRequestId, "http://localhost", cases);
        var path = $"tests/e2e/eh-{enhancementRequestId.ToString("N")[..8]}.spec.ts";

        var result = await _gitHub.UpsertBranchFileAsync(
            owner,
            repository,
            branch,
            path,
            spec,
            $"test: add Playwright specs for EnhancementHub request {enhancementRequestId}",
            cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Failed to export test cases for request {RequestId}: {Error}",
                enhancementRequestId,
                result.ErrorMessage);
            return;
        }

        _logger.LogInformation(
            "Exported {Count} test cases to {Owner}/{Repo}@{Branch}:{Path}",
            cases.Count,
            owner,
            repository,
            branch,
            path);
    }
}
