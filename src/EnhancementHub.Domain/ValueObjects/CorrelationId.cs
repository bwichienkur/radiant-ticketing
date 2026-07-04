namespace EnhancementHub.Domain.ValueObjects;

public readonly record struct CorrelationId(Guid Value)
{
    public static CorrelationId New() => new(Guid.NewGuid());

    public static CorrelationId From(Guid value) => new(value);

    public static CorrelationId? FromNullable(Guid? value) =>
        value.HasValue ? new CorrelationId(value.Value) : null;

    public override string ToString() => Value.ToString();
}
