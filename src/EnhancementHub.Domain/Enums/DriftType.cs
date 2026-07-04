namespace EnhancementHub.Domain.Enums;

public enum DriftType
{
    MissingInDatabase = 0,
    MissingInCode = 1,
    ColumnTypeMismatch = 2,
    NullableMismatch = 3,
    MigrationDrift = 4,
    OrphanTable = 5
}
