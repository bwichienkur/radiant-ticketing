namespace EnhancementHub.Application.Abstractions;

public interface IFeatureService
{
    bool IsEnabled(string featureName);
}

public static class FeatureFlags
{
    public const string IntakeCopilot = "IntakeCopilot";
    public const string GlobalSearch = "GlobalSearch";
    public const string SemanticSearch = "SemanticSearch";
    public const string FeedbackWidget = "FeedbackWidget";
    public const string ApprovalCopilot = "ApprovalCopilot";
}
