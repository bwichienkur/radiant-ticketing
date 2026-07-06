using System.Text.Json;
using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public static class TestCasePlanParser
{
    public sealed record ParsedTestCase(string Title, string? Description, IReadOnlyList<TestCaseStepDefinition> Steps);

    public static IReadOnlyList<ParsedTestCase> ParseTestingPlan(
        string? testingPlan,
        string desiredOutcome,
        string requestTitle)
    {
        if (string.IsNullOrWhiteSpace(testingPlan))
        {
            return [];
        }

        var cases = new List<ParsedTestCase>();
        var lines = testingPlan
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 1)
        {
            cases.Add(new ParsedTestCase(
                $"Verify: {Truncate(lines[0], 80)}",
                lines[0],
                [new TestCaseStepDefinition(1, lines[0], desiredOutcome)]));
            return cases;
        }

        var order = 1;
        foreach (var line in lines.Take(8))
        {
            var title = line.TrimStart('-', '*', ' ', '\t');
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            cases.Add(new ParsedTestCase(
                Truncate(title, 120),
                title,
                [new TestCaseStepDefinition(order++, $"Execute: {title}", desiredOutcome)]));
        }

        if (cases.Count == 0)
        {
            cases.Add(new ParsedTestCase(
                $"Validate: {Truncate(requestTitle, 80)}",
                desiredOutcome,
                [new TestCaseStepDefinition(1, "Validate desired outcome", desiredOutcome)]));
        }

        return cases;
    }

    public static IReadOnlyList<TestCaseStepDefinition> BuildSmokeSteps(string desiredOutcome, string? testingPlan)
    {
        var steps = new List<TestCaseStepDefinition>
        {
            new(1, "Open test environment", "Application loads successfully"),
            new(2, "Verify health endpoint", "HTTP 200 from /health"),
        };

        if (!string.IsNullOrWhiteSpace(testingPlan))
        {
            var order = 3;
            foreach (var line in testingPlan.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(4))
            {
                steps.Add(new TestCaseStepDefinition(order++, $"Execute: {line.TrimStart('-', '*', ' ')}", "Step passes"));
            }
        }

        steps.Add(new TestCaseStepDefinition(steps.Count + 1, "Validate desired outcome", desiredOutcome));
        return steps;
    }

    public static IReadOnlyList<TestCaseStepDefinition> DeserializeSteps(string? stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TestCaseStepDefinition>>(stepsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 3)] + "...";
}
