using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase82SecurityAttestationTests
{
    [Fact]
    public void ScimRotationScript_ExistsAndDocumentsSteps()
    {
        var script = File.ReadAllText(GetPath("scripts/rotate-scim-bearer-token.sh"));
        script.Should().Contain("Scim__BearerToken");
        script.Should().Contain("openssl rand");
        script.Should().Contain("Entra");
    }

    [Fact]
    public void SecurityDoc_DocumentsScimRotationAndProductionCsp()
    {
        var security = File.ReadAllText(GetPath("docs/SECURITY.md"));
        security.Should().Contain("rotate-scim-bearer-token.sh");
        security.Should().Contain("unsafe-eval");
        security.Should().Contain("Production");
    }

    [Fact]
    public void SecurityHeadersMiddleware_OmitsUnsafeEvalWhenDisabled()
    {
        var middleware = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Middleware/SecurityHeadersMiddleware.cs"));
        middleware.Should().Contain("allowUnsafeEval");
        middleware.Should().Contain("script-src 'self' 'unsafe-inline'; ");

        var program = File.ReadAllText(GetPath("src/EnhancementHub.Web/Program.cs"));
        program.Should().Contain("UseSecurityHeaders(allowUnsafeEval: app.Environment.IsDevelopment())");
    }

    [Fact]
    public void DeploymentGuide_IncludesSecretsManagerSection()
    {
        var deployment = File.ReadAllText(GetPath("docs/DEPLOYMENT.md"));
        deployment.Should().Contain("Secrets manager");
        deployment.Should().Contain("Azure Key Vault");
        deployment.Should().Contain("AWS Secrets Manager");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
