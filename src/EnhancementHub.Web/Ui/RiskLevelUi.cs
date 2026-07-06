using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Web.Ui;

public static class RiskLevelUi
{
    public static string BadgeClass(RiskLevel? risk) =>
        risk switch
        {
            RiskLevel.Critical => "text-bg-danger",
            RiskLevel.High => "text-bg-warning",
            RiskLevel.Medium => "text-bg-info",
            RiskLevel.Low => "text-bg-success",
            _ => "text-bg-secondary",
        };

    public static string PlainLabel(RiskLevel? risk) =>
        risk switch
        {
            RiskLevel.Critical => "Very high impact",
            RiskLevel.High => "High impact",
            RiskLevel.Medium => "Moderate impact",
            RiskLevel.Low => "Low impact",
            _ => "Unknown impact",
        };
}
