using System.Diagnostics;
using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class GitRepositoryHistoryService : IGitRepositoryHistoryService
{
    private readonly ILogger<GitRepositoryHistoryService> _logger;

    public GitRepositoryHistoryService(ILogger<GitRepositoryHistoryService> logger) =>
        _logger = logger;

    public string? GetHeadCommitHash(string repositoryPath)
    {
        if (!IsGitRepository(repositoryPath))
        {
            return null;
        }

        var result = RunGit(repositoryPath, "rev-parse HEAD");
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    public Task<GitRepositoryChanges> GetChangesSinceAsync(
        string repositoryPath,
        string sinceCommitHash,
        CancellationToken cancellationToken = default)
    {
        if (!IsGitRepository(repositoryPath))
        {
            return Task.FromResult(new GitRepositoryChanges(
                RequiresFullReindex: true,
                ChangedPaths: [],
                DeletedPaths: [],
                HeadCommitHash: null));
        }

        var headCommit = GetHeadCommitHash(repositoryPath);
        if (string.IsNullOrWhiteSpace(headCommit))
        {
            return Task.FromResult(new GitRepositoryChanges(
                RequiresFullReindex: true,
                ChangedPaths: [],
                DeletedPaths: [],
                HeadCommitHash: null));
        }

        if (string.Equals(headCommit, sinceCommitHash, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new GitRepositoryChanges(
                RequiresFullReindex: false,
                ChangedPaths: [],
                DeletedPaths: [],
                HeadCommitHash: headCommit));
        }

        var verify = RunGit(repositoryPath, $"cat-file -e {sinceCommitHash}^{{commit}}");
        if (verify.ExitCode != 0)
        {
            _logger.LogWarning(
                "Previous indexed commit {SinceCommit} not found in {RepositoryPath}; falling back to full reindex",
                sinceCommitHash,
                repositoryPath);

            return Task.FromResult(new GitRepositoryChanges(
                RequiresFullReindex: true,
                ChangedPaths: [],
                DeletedPaths: [],
                HeadCommitHash: headCommit));
        }

        var diff = RunGit(repositoryPath, $"diff --name-status {sinceCommitHash}..HEAD");
        if (diff.ExitCode != 0)
        {
            _logger.LogWarning(
                "Git diff failed for {RepositoryPath} ({SinceCommit}..HEAD); falling back to full reindex",
                repositoryPath,
                sinceCommitHash);

            return Task.FromResult(new GitRepositoryChanges(
                RequiresFullReindex: true,
                ChangedPaths: [],
                DeletedPaths: [],
                HeadCommitHash: headCommit));
        }

        var changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in diff.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ParseDiffLine(line, changed, deleted);
        }

        return Task.FromResult(new GitRepositoryChanges(
            RequiresFullReindex: false,
            ChangedPaths: changed.ToList(),
            DeletedPaths: deleted.ToList(),
            HeadCommitHash: headCommit));
    }

    internal static void ParseDiffLine(string line, ISet<string> changed, ISet<string> deleted)
    {
        var parts = line.Split('\t', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return;
        }

        var status = parts[0][0];
        switch (status)
        {
            case 'M':
            case 'A':
            case 'C':
            case 'T':
                changed.Add(NormalizePath(parts[^1]));
                break;
            case 'D':
                deleted.Add(NormalizePath(parts[1]));
                break;
            case 'R':
            case 'U':
                if (parts.Length >= 3)
                {
                    deleted.Add(NormalizePath(parts[1]));
                    changed.Add(NormalizePath(parts[2]));
                }
                break;
        }
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static bool IsGitRepository(string repositoryPath) =>
        Directory.Exists(Path.Combine(repositoryPath, ".git"));

    private static GitCommandResult RunGit(string workingDirectory, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new GitCommandResult(process.ExitCode, output);
    }

    private readonly record struct GitCommandResult(int ExitCode, string Output);
}
