using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class SimulatedQaRunner : IQaRunner
{
    private readonly IFileStorageService _fileStorage;

    public SimulatedQaRunner(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public QaRunnerKind RunnerKind => QaRunnerKind.Simulated;

    public async Task<QaEvidenceResult> RunAsync(QaRunManifest manifest, CancellationToken cancellationToken = default)
    {
        var caseResults = new List<QaCaseRunResult>();
        var allSteps = new List<QaTestStepResult>
        {
            new("Open test environment", true, $"Loaded {manifest.TestUrl}"),
            new("Verify health endpoint", true, "HTTP 200 from /health"),
        };

        foreach (var manifestCase in manifest.Cases)
        {
            var caseSteps = new List<QaTestStepResult>();
            foreach (var step in manifestCase.Steps.OrderBy(s => s.Order))
            {
                var detail = step.ExpectedResult ?? "Automated check passed";
                caseSteps.Add(new QaTestStepResult(step.Action, true, detail));
                allSteps.Add(new QaTestStepResult(
                    $"{manifestCase.Title}: {step.Action}",
                    true,
                    detail));
            }

            if (caseSteps.Count == 0)
            {
                caseSteps.Add(new QaTestStepResult("Execute case", true, manifest.DesiredOutcome));
                allSteps.Add(new QaTestStepResult(manifestCase.Title, true, manifest.DesiredOutcome));
            }

            caseResults.Add(new QaCaseRunResult(
                manifestCase.TestCaseId,
                manifestCase.TestCaseVersionId,
                manifestCase.Title,
                manifestCase.IsRegressionCase,
                true,
                250,
                "Simulated pass",
                caseSteps,
                null));
        }

        var passed = caseResults.All(c => c.Passed);
        var container = $"delivery/{manifest.EnhancementRequestId}/{manifest.DeliveryRunId}";

        var reportHtml = BuildReportHtml(manifest, caseResults);
        await using var reportStream = new MemoryStream(Encoding.UTF8.GetBytes(reportHtml));
        var reportPath = await _fileStorage.SaveAsync(
            container,
            "qa-report.html",
            reportStream,
            "text/html",
            cancellationToken);

        var videoHtml = BuildVideoWalkthroughHtml(manifest.TestUrl, allSteps);
        await using var videoStream = new MemoryStream(Encoding.UTF8.GetBytes(videoHtml));
        var videoPath = await _fileStorage.SaveAsync(
            container,
            "qa-walkthrough.html",
            videoStream,
            "text/html",
            cancellationToken);

        return new QaEvidenceResult(
            passed,
            allSteps,
            caseResults,
            videoPath,
            reportPath,
            QaRunnerKind.Simulated,
            true);
    }

    private static string BuildReportHtml(QaRunManifest manifest, IReadOnlyList<QaCaseRunResult> cases)
    {
        var rows = string.Join(
            "",
            cases.SelectMany(c => c.Steps.Select(s =>
                $"<tr><td>{Encode(c.Title)}</td><td>{Encode(s.Step)}</td><td>{(s.Passed ? "Pass" : "Fail")}</td><td>{Encode(s.Detail)}</td></tr>")));

        return $"""
            <!DOCTYPE html><html><head><title>QA Report</title></head><body>
            <h1>QA evidence</h1>
            <p>Target: {Encode(manifest.TestUrl)}</p>
            <p>Cases: {cases.Count} ({cases.Count(c => c.IsRegressionCase)} regression)</p>
            <table border="1" cellpadding="6">
            <thead><tr><th>Case</th><th>Step</th><th>Result</th><th>Detail</th></tr></thead>
            <tbody>{rows}</tbody></table></body></html>
            """;
    }

    private static string BuildVideoWalkthroughHtml(string testUrl, IReadOnlyList<QaTestStepResult> steps)
    {
        var stepList = string.Join(
            "",
            steps.Select((s, i) => $"<li>{i + 1}. {Encode(s.Step)}</li>"));

        return "<!DOCTYPE html><html><head><title>QA Walkthrough</title>" +
            "<style>body { font-family: sans-serif; max-width: 720px; margin: 2rem auto; }</style></head><body>" +
            "<h1>Simulated QA walkthrough</h1>" +
            $"<p>This artifact documents the automated UI verification against <strong>{Encode(testUrl)}</strong>.</p>" +
            $"<ol>{stepList}</ol>" +
            "<p><em>Replace with Playwright-recorded video in production deployments.</em></p>" +
            "</body></html>";
    }

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
