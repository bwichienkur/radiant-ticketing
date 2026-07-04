namespace EnhancementHub.Domain.Enums;

public enum ConfidenceLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

public static class ConfidenceLevelExtensions
{
    public static ConfidenceLevel FromScore(double score) => score switch
    {
        >= 0.9 => ConfidenceLevel.VeryHigh,
        >= 0.75 => ConfidenceLevel.High,
        >= 0.5 => ConfidenceLevel.Medium,
        >= 0.25 => ConfidenceLevel.Low,
        _ => ConfidenceLevel.VeryLow
    };

    public static double ToMinimumScore(this ConfidenceLevel level) => level switch
    {
        ConfidenceLevel.VeryHigh => 0.9,
        ConfidenceLevel.High => 0.75,
        ConfidenceLevel.Medium => 0.5,
        ConfidenceLevel.Low => 0.25,
        _ => 0.0
    };
}
