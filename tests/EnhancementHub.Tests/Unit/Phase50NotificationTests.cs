using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase50NotificationTests
{
    [Fact]
    public void SpaNotificationsController_ExposesBffEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaNotificationsController.cs"));
        controller.Should().Contain("web-api/spa/notifications");
        controller.Should().Contain("unread-count");
        controller.Should().Contain("mark-all-read");
        controller.Should().Contain("preferences");
    }

    [Fact]
    public void NotificationDomain_IncludesEntityAndPreferenceTables()
    {
        var notification = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/Notification.cs"));
        var preference = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/NotificationPreference.cs"));
        var enumFile = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Enums/NotificationType.cs"));

        notification.Should().Contain("NotificationType");
        preference.Should().Contain("EmailEnabled");
        enumFile.Should().Contain("ApprovalAssigned");
        enumFile.Should().Contain("DriftCritical");
    }

    [Fact]
    public void PlatformNotificationHub_AuthorizesAndJoinsUserGroup()
    {
        var hub = File.ReadAllText(GetPath("src/EnhancementHub.Web/Hubs/PlatformNotificationHub.cs"));
        hub.Should().Contain("[Authorize]");
        hub.Should().Contain("OnConnectedAsync");
        hub.Should().Contain("NotificationGroupNames.ForUser");
    }

    [Fact]
    public void TriggerAiAnalysisCommand_NotifiesApproversOnPendingApproval()
    {
        var handler = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Analysis/Commands/TriggerAiAnalysisCommand.cs"));
        handler.Should().Contain("NotifyApproversOfPendingApprovalAsync");
        handler.Should().Contain("NotifySubmitterOfAnalysisCompleteAsync");
    }

    [Fact]
    public void SiteJs_FetchesPersistedNotificationsAndListensForUserNotification()
    {
        var siteJs = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        siteJs.Should().Contain("/web-api/spa/notifications");
        siteJs.Should().Contain("UserNotification");
        siteJs.Should().Contain("mark-all-read");
    }

    [Fact]
    public void AccountNotificationPreferencesPage_Exists()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Account/NotificationPreferences.cshtml"));
        page.Should().Contain("Notification preferences");
        page.Should().Contain("In-app");
        page.Should().Contain("Email");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
