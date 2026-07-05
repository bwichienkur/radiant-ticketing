using System.Data.Common;
using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnhancementHub.Infrastructure.Persistence;

public sealed class TenantSearchPathConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSearchPathConnectionInterceptor(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplySearchPath(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplySearchPathAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void ApplySearchPath(DbConnection connection)
    {
        var schemaName = ResolveActiveSchemaName();
        if (string.IsNullOrWhiteSpace(schemaName) || connection is not Npgsql.NpgsqlConnection)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = Services.TenantSchemaProvisioner.BuildSearchPathSql(schemaName);
        command.ExecuteNonQuery();
    }

    private async Task ApplySearchPathAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var schemaName = ResolveActiveSchemaName();
        if (string.IsNullOrWhiteSpace(schemaName) || connection is not Npgsql.NpgsqlConnection)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = Services.TenantSchemaProvisioner.BuildSearchPathSql(schemaName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string? ResolveActiveSchemaName() =>
        _httpContextAccessor.HttpContext?
            .RequestServices
            .GetService(typeof(ITenantSchemaAccessor)) is ITenantSchemaAccessor accessor
            ? accessor.ActiveSchemaName
            : null;
}
