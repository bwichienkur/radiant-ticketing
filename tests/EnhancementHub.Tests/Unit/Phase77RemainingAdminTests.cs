using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase77RemainingAdminTests
{
    [Theory]
    [InlineData("Tenancy.cshtml.cs", "/Spa/Admin/Tenancy")]
    [InlineData("Observability.cshtml.cs", "/Spa/Admin/Observability")]
    [InlineData("DataScaling.cshtml.cs", "/Spa/Admin/DataScaling")]
    [InlineData("Retention.cshtml.cs", "/Spa/Admin/Retention")]
    [InlineData("Delivery.cshtml.cs", "/Spa/Admin/Delivery")]
    [InlineData("AiPrompts.cshtml.cs", "/Spa/Admin/AiPrompts")]
    public void LegacyAdminPages_RedirectToSpa(string fileName, string spaRoute)
    {
        var page = File.ReadAllText(GetPath($"src/EnhancementHub.Web/Pages/Admin/{fileName}"));
        page.Should().Contain("[Obsolete");
        page.Should().Contain(spaRoute);
    }

    [Fact]
    public void AdminApp_IncludesRemainingSections()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/AdminApp.tsx"));
        app.Should().Contain("AdminTenancySection");
        app.Should().Contain("AdminObservabilitySection");
        app.Should().Contain("AdminDataScalingSection");
        app.Should().Contain("AdminRetentionSection");
        app.Should().Contain("AdminDeliverySection");
        app.Should().Contain("AdminAiPromptsSection");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
