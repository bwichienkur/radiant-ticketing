namespace EnhancementHub.Infrastructure.Persistence;

/// <summary>
/// Resolves relative SQLite paths against the application content root so the database
/// location is stable regardless of process working directory.
/// </summary>
public static class SqliteConnectionStringResolver
{
    public static string Resolve(string connectionString, string contentRootPath)
    {
        const string prefix = "Data Source=";
        var idx = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return connectionString;
        }

        var start = idx + prefix.Length;
        var end = connectionString.IndexOf(';', start);
        var dataSource = (end < 0 ? connectionString[start..] : connectionString[start..end]).Trim();

        if (Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        var resolved = Path.GetFullPath(Path.Combine(contentRootPath, dataSource));
        return end < 0
            ? connectionString[..start] + resolved
            : connectionString[..start] + resolved + connectionString[end..];
    }
}
