using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase62EnterpriseIntakeTests
{
    [Fact]
    public void SpaRequestsController_IncludesCustomFieldsOnCreateForm()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaRequestsController.cs"));
        controller.Should().Contain("ListCustomFieldDefinitionsQuery");
        controller.Should().Contain("CustomFields");
    }

    [Fact]
    public void SpaContracts_ExposesCustomFieldInput()
    {
        var contracts = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaContracts.cs"));
        contracts.Should().Contain("SpaCustomFieldValueInput");
        contracts.Should().Contain("CustomFieldDefinitionDto");
    }

    [Fact]
    public void CreateRequestApp_RendersCustomFields()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        app.Should().Contain("customFieldDefinitions");
        app.Should().Contain("buildCustomFieldPayload");
        app.Should().Contain("Additional fields");
    }

    [Fact]
    public void SpaShell_IncludesMockAiTrustBanner()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("MockAiTrustBanner");
    }

    [Fact]
    public void MockAiTrustBanner_UsesRuntimeStatus()
    {
        var banner = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/MockAiTrustBanner.tsx"));
        banner.Should().Contain("usesSimulatedBackends");
        banner.Should().Contain("Simulated AI mode");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
