using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class DatabaseSchemaScannerFactory
{
    private readonly SqlServerSchemaScanner _sqlServer;
    private readonly PostgreSqlSchemaScanner _postgreSql;

    public DatabaseSchemaScannerFactory(
        SqlServerSchemaScanner sqlServer,
        PostgreSqlSchemaScanner postgreSql)
    {
        _sqlServer = sqlServer;
        _postgreSql = postgreSql;
    }

    public IDatabaseSchemaScanner GetScanner(DatabaseProviderType provider) =>
        provider switch
        {
            DatabaseProviderType.SqlServer => _sqlServer,
            DatabaseProviderType.PostgreSQL => _postgreSql,
            _ => throw new NotSupportedException($"Database provider {provider} is not supported.")
        };

    public Task<DatabaseSchemaScanResult> ScanAsync(
        string connectionString,
        DatabaseProviderType provider,
        CancellationToken cancellationToken = default) =>
        GetScanner(provider).ScanAsync(connectionString, provider, cancellationToken);
}
