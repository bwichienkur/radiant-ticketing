using EnhancementHub.Infrastructure.Persistence;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class SqliteConnectionStringResolverTests
{
    [Fact]
    public void Resolve_CombinesRelativeDataSourceWithContentRoot()
    {
        var resolved = SqliteConnectionStringResolver.Resolve(
            "Data Source=enhancementhub.db",
            "/app/src/EnhancementHub.Web");

        resolved.Should().Be("Data Source=/app/src/EnhancementHub.Web/enhancementhub.db");
    }

    [Fact]
    public void Resolve_LeavesAbsoluteDataSourceUnchanged()
    {
        var resolved = SqliteConnectionStringResolver.Resolve(
            "Data Source=/var/data/app.db;Cache=Shared",
            "/app");

        resolved.Should().Be("Data Source=/var/data/app.db;Cache=Shared");
    }
}
