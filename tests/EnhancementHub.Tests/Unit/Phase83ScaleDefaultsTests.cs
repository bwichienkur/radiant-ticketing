using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase83ScaleDefaultsTests
{
    [Fact]
    public void ProductionDockerCompose_EnablesObservabilityByDefault()
    {
        var compose = File.ReadAllText(GetPath("docker-compose.prod.yml"));
        compose.Should().Contain("Observability__Enabled: \"true\"");
        compose.Should().Contain("otel-collector");
    }

    [Fact]
    public void HelmValues_EnableObservabilityInProdSamples()
    {
        var values = File.ReadAllText(GetPath("deploy/helm/enhancementhub/values.yaml"));
        values.Should().Contain("observabilityEnabled: true");

        var valuesHa = File.ReadAllText(GetPath("deploy/helm/enhancementhub/values-ha.yaml"));
        valuesHa.Should().Contain("observabilityEnabled: true");
    }

    [Fact]
    public void ConfigurationFeatureService_UsesMemoryCacheAndEmitsMetrics()
    {
        var service = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/ConfigurationFeatureService.cs"));
        service.Should().Contain("IMemoryCache");
        service.Should().Contain("FeatureFlagCacheHits");
        service.Should().Contain("FeatureFlagCacheMisses");

        var telemetry = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Observability/EnhancementHubTelemetry.cs"));
        telemetry.Should().Contain("enhancementhub.feature_flag.cache.hits");
        telemetry.Should().Contain("enhancementhub.feature_flag.cache.misses");
    }

    [Fact]
    public void LoadSmoke_IsBlockingInCi()
    {
        var ci = File.ReadAllText(GetPath(".github/workflows/ci.yml"));
        ci.Should().Contain("load-smoke:");
        ci.Should().NotContain("continue-on-error: true");
    }

    [Fact]
    public void VisualRegressionWorkflow_Exists()
    {
        File.Exists(GetPath(".github/workflows/visual-regression.yml")).Should().BeTrue();
        var workflow = File.ReadAllText(GetPath(".github/workflows/visual-regression.yml"));
        workflow.Should().Contain("storybook-visual-smoke.mjs");
    }

    [Fact]
    public void SpaBffTests_CoverCoreRoutes()
    {
        var tests = File.ReadAllText(GetPath("tests/EnhancementHub.Tests/Integration/SpaBffTests.cs"));
        tests.Should().Contain("SpaAdminController_ExposesCoreBffRoutes");
        tests.Should().Contain("SpaPortfolioController_ExposesHealthAndExport");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
