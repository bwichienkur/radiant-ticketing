using System.Net;
using System.Net.Http.Json;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

[Collection("Integration")]
public sealed class RolePermissionTests
{
    private readonly TestWebApplicationFactory _factory;

    public RolePermissionTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Approve_ReturnsForbidden_ForSubmitterRole()
    {
        var builder = _factory.CreateDataBuilder();
        var submitter = await builder.CreateUserAsync(UserRole.Submitter);
        var request = await builder.CreateEnhancementRequestAsync(
            submitter,
            EnhancementRequestStatus.PendingApproval);

        var client = await _factory.CreateAuthenticatedClientAsync(submitter);
        var response = await client.PostAsJsonAsync(
            $"/api/approvals/{request.Id}/actions",
            new { actionType = ApprovalActionType.Approve, comments = "Self approve" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Approver or Admin");
    }

    [Fact]
    public async Task AdminSettings_ReturnsForbidden_ForNonAdminRole()
    {
        var builder = _factory.CreateDataBuilder();
        var reviewer = await builder.CreateUserAsync(UserRole.Reviewer);
        var client = await _factory.CreateAuthenticatedClientAsync(reviewer);

        var response = await client.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminSettings_ReturnsOk_ForAdminRole()
    {
        var builder = _factory.CreateDataBuilder();
        var admin = await builder.CreateUserAsync(UserRole.Admin);
        var client = await _factory.CreateAuthenticatedClientAsync(admin);

        var response = await client.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
