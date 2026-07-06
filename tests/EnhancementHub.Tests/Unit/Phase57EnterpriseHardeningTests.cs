using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase57EnterpriseHardeningTests
{
    [Fact]
    public void ScimUsersController_ExposesScimV2Endpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Api/Controllers/ScimUsersController.cs"));
        controller.Should().Contain("scim/v2/Users");
        controller.Should().Contain("ProvisionScimUserCommand");
        controller.Should().Contain("DeactivateScimUserCommand");
    }

    [Fact]
    public void UserEntity_IncludesScimExternalId()
    {
        var user = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/User.cs"));
        user.Should().Contain("ExternalId");
        user.Should().Contain("ProvisionedViaScim");
    }

    [Fact]
    public void CustomFieldTypes_SupportFiveFieldTypes()
    {
        var enumFile = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Enums/CustomFieldType.cs"));
        enumFile.Should().Contain("Text");
        enumFile.Should().Contain("Select");
        enumFile.Should().Contain("Number");
        enumFile.Should().Contain("Date");
        enumFile.Should().Contain("User");

        File.Exists(GetPath("src/EnhancementHub.Web/Pages/Admin/CustomFields.cshtml")).Should().BeTrue();
        File.Exists(GetPath("src/EnhancementHub.Api/Controllers/CustomFieldsController.cs")).Should().BeTrue();
    }

    [Fact]
    public void ApprovalPolicyRule_IncludesSlaEscalationFields()
    {
        var entity = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/ApprovalPolicyRule.cs"));
        entity.Should().Contain("SlaTargetHours");
        entity.Should().Contain("EscalateOnBreach");
        entity.Should().Contain("EscalateToRole");

        var job = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Background/Executors/SlaEscalationJobExecutor.cs"));
        job.Should().Contain("SlaEscalationService");

        var notificationType = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Enums/NotificationType.cs"));
        notificationType.Should().Contain("SlaEscalation");
    }

    [Fact]
    public void SecurityHeadersMiddleware_ExistsAndRegistered()
    {
        var middleware = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Middleware/SecurityHeadersMiddleware.cs"));
        middleware.Should().Contain("Content-Security-Policy");
        middleware.Should().Contain("X-Frame-Options");

        var webProgram = File.ReadAllText(GetPath("src/EnhancementHub.Web/Program.cs"));
        webProgram.Should().Contain("UseSecurityHeaders");
    }

    [Fact]
    public void CiWorkflow_ChecksVulnerablePackages()
    {
        var ci = File.ReadAllText(GetPath(".github/workflows/ci.yml"));
        ci.Should().Contain("dotnet list package --vulnerable");
        ci.Should().Contain("critical");
    }

    [Fact]
    public void CodeQlWorkflow_Exists()
    {
        File.Exists(GetPath(".github/workflows/codeql.yml")).Should().BeTrue();
    }

    [Fact]
    public void AuditController_ExposesSignedExportApi()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Api/Controllers/AuditController.cs"));
        controller.Should().Contain("api/audit");
        controller.Should().Contain("RequestAuditExportCommand");
        controller.Should().Contain("download");
        controller.Should().Contain("IAuditExportTokenService");
    }

    [Fact]
    public void SecurityDoc_MentionsCspAndScim()
    {
        var security = File.ReadAllText(GetPath("docs/SECURITY.md"));
        security.Should().Contain("Content Security Policy");
        security.Should().Contain("SCIM");
        security.Should().Contain("audit/export");
    }

    [Fact]
    public void Phase57Migration_Exists()
    {
        Directory.GetFiles(GetPath("src/EnhancementHub.Infrastructure/Migrations"), "*Phase57*")
            .Should().NotBeEmpty();
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
