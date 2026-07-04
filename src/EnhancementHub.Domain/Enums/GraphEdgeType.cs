namespace EnhancementHub.Domain.Enums;

public enum GraphEdgeType
{
    ForeignKey = 0,
    MapsTo = 1,
    References = 2,
    Calls = 3,
    Contains = 4,
    DependsOn = 5
}
