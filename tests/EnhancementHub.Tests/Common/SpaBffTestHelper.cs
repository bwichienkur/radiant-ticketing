namespace EnhancementHub.Tests.Common;

public static class SpaBffTestHelper
{
    public static string ReadAllSpaBffSources()
    {
        var spaDir = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa");
        if (!Directory.Exists(spaDir))
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(spaDir, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }
}
