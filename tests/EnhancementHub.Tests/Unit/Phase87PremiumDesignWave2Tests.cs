using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase87PremiumDesignWave2Tests
{
    [Fact]
    public void PremiumCss_IncludesTableFormAdminAndInsightStrip()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-premium-v3.css"));
        css.Should().Contain(".eh-table");
        css.Should().Contain(".eh-form-field");
        css.Should().Contain(".eh-admin");
        css.Should().Contain(".eh-insight-strip");
        css.Should().Contain(".login-hero");
    }

    [Fact]
    public void FormField_AppliesInvalidStateToControls()
    {
        var formField = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/FormField.tsx"));
        formField.Should().Contain("eh-form-field--invalid");
        formField.Should().Contain("is-invalid");
        formField.Should().Contain("cloneElement");
    }

    [Fact]
    public void CreateRequest_UsesSectionCardAndFieldValidation()
    {
        var create = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        create.Should().Contain("eh-create-request");
        create.Should().Contain("SectionCard");
        create.Should().Contain("validateForm");
        create.Should().Contain("fieldErrors");
    }

    [Fact]
    public void Dashboard_IncludesInsightStrip()
    {
        var dashboard = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/DashboardApp.tsx"));
        var strip = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/DashboardInsightStrip.tsx"));
        dashboard.Should().Contain("DashboardInsightStrip");
        strip.Should().Contain("eh-insight-strip");
        strip.Should().Contain("buildInsights");
    }

    [Fact]
    public void AdminApp_UsesPremiumWrapper()
    {
        var admin = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/AdminApp.tsx"));
        admin.Should().Contain("eh-admin");
    }

    [Fact]
    public void StorybookSmoke_IncludesFormAndEmptyStateStories()
    {
        var script = File.ReadAllText(GetPath("scripts/storybook-visual-smoke.mjs"));
        script.Should().Contain("ui-kit-components--form-field-example");
        script.Should().Contain("ui-kit-components--empty-state-inbox");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
