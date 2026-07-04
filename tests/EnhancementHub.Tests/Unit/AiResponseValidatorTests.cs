using EnhancementHub.Infrastructure.Services;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class AiResponseValidatorTests
{
    private readonly AiResponseValidator _sut = new();

    [Fact]
    public void TryValidate_WithValidJson_ReturnsParsedResult()
    {
        const string json = """
            {
              "summary": "Add export endpoint for orders.",
              "impactedAreas": ["API"],
              "recommendations": ["Add integration test"],
              "risks": ["Breaking change"],
              "estimatedEffortHours": 12,
              "modelUsed": "gpt-test",
              "isMock": false
            }
            """;

        var success = _sut.TryValidate(json, out var result, out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        result.Should().NotBeNull();
        result!.Summary.Should().Be("Add export endpoint for orders.");
        result.ImpactedAreas.Should().ContainSingle("API");
        result.Recommendations.Should().ContainSingle("Add integration test");
        result.Risks.Should().ContainSingle("Breaking change");
        result.EstimatedEffortHours.Should().Be(12);
    }

    [Fact]
    public void TryValidate_WithMissingSummary_ReturnsFalse()
    {
        const string json = """{ "estimatedEffortHours": 5 }""";

        var success = _sut.TryValidate(json, out _, out var error);

        success.Should().BeFalse();
        error.Should().Be("Missing or invalid 'summary' property.");
    }

    [Fact]
    public void TryValidate_WithEmptySummary_ReturnsFalse()
    {
        const string json = """{ "summary": "   " }""";

        var success = _sut.TryValidate(json, out _, out var error);

        success.Should().BeFalse();
        error.Should().Be("Summary cannot be empty.");
    }

    [Fact]
    public void TryValidate_WithInvalidJson_ReturnsFalse()
    {
        var success = _sut.TryValidate("{ not json", out _, out var error);

        success.Should().BeFalse();
        error.Should().StartWith("Invalid JSON:");
    }

    [Fact]
    public void TryValidate_WithEmptyInput_ReturnsFalse()
    {
        var success = _sut.TryValidate("  ", out _, out var error);

        success.Should().BeFalse();
        error.Should().Be("Empty AI response.");
    }

    [Fact]
    public void TryValidate_WithNullCollections_DefaultsToEmptyArrays()
    {
        const string json = """{ "summary": "Valid summary" }""";

        var success = _sut.TryValidate(json, out var result, out _);

        success.Should().BeTrue();
        result!.ImpactedAreas.Should().BeEmpty();
        result.Recommendations.Should().BeEmpty();
        result.Risks.Should().BeEmpty();
    }
}
