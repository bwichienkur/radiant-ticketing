using EnhancementHub.Infrastructure.Services;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase14FutureEnhancementsTests
{
    [Fact]
    public void ResolveRepositoryRoot_UsesSingleTopLevelDirectory()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var repoRoot = Path.Combine(temp, "MyRepo");
        Directory.CreateDirectory(repoRoot);

        try
        {
            RepositoryArchiveExtractService.ResolveRepositoryRoot(temp).Should().Be(repoRoot);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void ResolveRepositoryRoot_UsesExtractRootWhenMultipleDirectories()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(temp, "A"));
        Directory.CreateDirectory(Path.Combine(temp, "B"));

        try
        {
            RepositoryArchiveExtractService.ResolveRepositoryRoot(temp).Should().Be(temp);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }
}
