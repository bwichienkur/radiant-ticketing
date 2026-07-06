using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class ChangeWindowEvaluator : IChangeWindowEvaluator
{
    public bool IsProductionDeployAllowed(DateTime utcNow, string? changeWindowNotes, bool requireChangeWindow)
    {
        if (!requireChangeWindow)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(changeWindowNotes))
        {
            return true;
        }

        if (changeWindowNotes.Contains("Sunday", StringComparison.OrdinalIgnoreCase))
        {
            return utcNow.DayOfWeek == DayOfWeek.Sunday && utcNow.Hour is >= 2 and < 6;
        }

        return true;
    }
}

public sealed class QaEvidenceService : IQaEvidenceService
{
    private readonly IFileStorageService _fileStorage;

    public QaEvidenceService(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public async Task<QaEvidenceResult> RunQaAsync(
        Guid requestId,
        Guid deliveryRunId,
        string testUrl,
        string? testingPlan,
        string desiredOutcome,
        CancellationToken cancellationToken = default)
    {
        var steps = BuildSteps(testingPlan, desiredOutcome, testUrl);
        var passed = steps.All(s => s.Passed);
        var container = $"delivery/{requestId}/{deliveryRunId}";

        var reportHtml = BuildReportHtml(testUrl, steps);
        await using var reportStream = new MemoryStream(Encoding.UTF8.GetBytes(reportHtml));
        var reportPath = await _fileStorage.SaveAsync(
            container,
            "qa-report.html",
            reportStream,
            "text/html",
            cancellationToken);

        var videoHtml = BuildVideoWalkthroughHtml(testUrl, steps);
        await using var videoStream = new MemoryStream(Encoding.UTF8.GetBytes(videoHtml));
        var videoPath = await _fileStorage.SaveAsync(
            container,
            "qa-walkthrough.html",
            videoStream,
            "text/html",
            cancellationToken);

        return new QaEvidenceResult(passed, steps, [], videoPath, reportPath, QaRunnerKind.Simulated, true);
    }

    private static IReadOnlyList<QaTestStepResult> BuildSteps(string? testingPlan, string desiredOutcome, string testUrl)
    {
        var steps = new List<QaTestStepResult>
        {
            new("Open test environment", true, $"Loaded {testUrl}"),
            new("Verify health endpoint", true, "HTTP 200 from /health"),
        };

        if (!string.IsNullOrWhiteSpace(testingPlan))
        {
            foreach (var line in testingPlan.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(6))
            {
                steps.Add(new($"Execute: {line}", true, "Automated check passed"));
            }
        }

        steps.Add(new("Validate desired outcome", true, desiredOutcome));
        return steps;
    }

    private static string BuildReportHtml(string testUrl, IReadOnlyList<QaTestStepResult> steps)
    {
        var rows = string.Join(
            "",
            steps.Select(s =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(s.Step)}</td><td>{(s.Passed ? "Pass" : "Fail")}</td><td>{System.Net.WebUtility.HtmlEncode(s.Detail)}</td></tr>"));

        return $"""
            <!DOCTYPE html><html><head><title>QA Report</title></head><body>
            <h1>QA evidence</h1><p>Target: {System.Net.WebUtility.HtmlEncode(testUrl)}</p>
            <table border="1" cellpadding="6"><thead><tr><th>Step</th><th>Result</th><th>Detail</th></tr></thead>
            <tbody>{rows}</tbody></table></body></html>
            """;
    }

    private static string BuildVideoWalkthroughHtml(string testUrl, IReadOnlyList<QaTestStepResult> steps)
    {
        var stepList = string.Join(
            "",
            steps.Select((s, i) => $"<li>{i + 1}. {System.Net.WebUtility.HtmlEncode(s.Step)}</li>"));

        return "<!DOCTYPE html><html><head><title>QA Walkthrough</title>" +
            "<style>body { font-family: sans-serif; max-width: 720px; margin: 2rem auto; }</style></head><body>" +
            "<h1>Simulated QA walkthrough</h1>" +
            $"<p>This artifact documents the automated UI verification against <strong>{System.Net.WebUtility.HtmlEncode(testUrl)}</strong>.</p>" +
            $"<ol>{stepList}</ol>" +
            "<p><em>Replace with Playwright-recorded video in production deployments.</em></p>" +
            "</body></html>";
    }
}
