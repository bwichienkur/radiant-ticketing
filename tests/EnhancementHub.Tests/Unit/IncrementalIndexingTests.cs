using EnhancementHub.Infrastructure.Services;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class GitRepositoryHistoryServiceTests
{
    [Theory]
    [InlineData("M\tsrc/Foo.cs", "src/Foo.cs", null)]
    [InlineData("A\tsrc/New.cs", "src/New.cs", null)]
    [InlineData("D\tsrc/Old.cs", null, "src/Old.cs")]
    [InlineData("R100\tsrc/Old.cs\tsrc/Renamed.cs", "src/Renamed.cs", "src/Old.cs")]
    public void ParseDiffLine_ClassifiesGitStatus(string line, string? expectedChanged, string? expectedDeleted)
    {
        var changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        GitRepositoryHistoryService.ParseDiffLine(line, changed, deleted);

        if (expectedChanged is null)
        {
            changed.Should().BeEmpty();
        }
        else
        {
            changed.Should().ContainSingle().Which.Should().Be(expectedChanged);
        }

        if (expectedDeleted is null)
        {
            deleted.Should().BeEmpty();
        }
        else
        {
            deleted.Should().ContainSingle().Which.Should().Be(expectedDeleted);
        }
    }

    [Fact]
    public void GetHeadCommitHash_ReturnsCommitForWorkspaceRepository()
    {
        var service = new GitRepositoryHistoryService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GitRepositoryHistoryService>.Instance);

        var head = service.GetHeadCommitHash("/workspace");

        head.Should().NotBeNullOrWhiteSpace();
        head.Should().MatchRegex("^[0-9a-f]{40}$");
    }

    [Fact]
    public async Task GetChangesSinceAsync_ReturnsNoChanges_WhenSameCommit()
    {
        var service = new GitRepositoryHistoryService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GitRepositoryHistoryService>.Instance);
        var head = service.GetHeadCommitHash("/workspace");
        head.Should().NotBeNull();

        var changes = await service.GetChangesSinceAsync("/workspace", head!);

        changes.RequiresFullReindex.Should().BeFalse();
        changes.ChangedPaths.Should().BeEmpty();
        changes.DeletedPaths.Should().BeEmpty();
        changes.HeadCommitHash.Should().Be(head);
    }

    [Fact]
    public async Task GetChangesSinceAsync_DetectsChangedFilesAcrossCommits()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "eh-git-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            await RunGit(tempDir, "init");
            await RunGit(tempDir, "config user.email test@test.local");
            await RunGit(tempDir, "config user.name Test");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Sample.cs"), "class A {}");
            await RunGit(tempDir, "add .");
            await RunGit(tempDir, "commit -m init");

            var service = new GitRepositoryHistoryService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GitRepositoryHistoryService>.Instance);
            var firstCommit = service.GetHeadCommitHash(tempDir);

            await File.WriteAllTextAsync(Path.Combine(tempDir, "Sample.cs"), "class A { void M() {} }");
            await RunGit(tempDir, "add .");
            await RunGit(tempDir, "commit -m update");

            var changes = await service.GetChangesSinceAsync(tempDir, firstCommit!);

            changes.RequiresFullReindex.Should().BeFalse();
            changes.ChangedPaths.Should().Contain("Sample.cs");
            changes.HeadCommitHash.Should().NotBe(firstCommit);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GetChangesSinceAsync_RequiresFullReindex_WhenPreviousCommitMissing()
    {
        var service = new GitRepositoryHistoryService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GitRepositoryHistoryService>.Instance);

        var changes = await service.GetChangesSinceAsync("/workspace", "0000000000000000000000000000000000000000");

        changes.RequiresFullReindex.Should().BeTrue();
        changes.HeadCommitHash.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task RunGit(string workingDirectory, string arguments)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Failed to start git");

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"git {arguments} failed: {error}");
        }
    }
}
