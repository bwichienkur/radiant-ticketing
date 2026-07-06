using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Services.Delivery;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class PlaywrightSpecGeneratorTests
{
    [Fact]
    public void GenerateCombinedSpec_IncludesRequestCases()
    {
        var requestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var cases = new[]
        {
            new TestCaseExportItem(
                Guid.NewGuid(),
                "Verify export",
                """[{"Order":1,"Action":"Open reports","ExpectedResult":"Report list visible"}]""",
                "tests/e2e/eh-aaaaaaaa-verify-export.spec.ts")
        };

        var spec = PlaywrightSpecGenerator.GenerateCombinedSpec(requestId, "https://test.example.com", cases);

        spec.Should().Contain("@playwright/test");
        spec.Should().Contain("Verify export");
        spec.Should().Contain("EH_TEST_URL");
        spec.Should().Contain("https://test.example.com");
    }

    [Fact]
    public void SuggestRepositoryPath_UsesRequestPrefix()
    {
        var path = PlaywrightSpecGenerator.SuggestRepositoryPath(
            Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            "Cancel flow");

        path.Should().StartWith("tests/e2e/eh-12345678");
        path.Should().EndWith(".spec.ts");
    }
}
