using System.Diagnostics;
using System.Text;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class GitRepositoryCloneService : IGitRepositoryCloneService
{
    private readonly ILogger<GitRepositoryCloneService> _logger;
    private readonly string _cloneRoot;

    public GitRepositoryCloneService(IConfiguration configuration, ILogger<GitRepositoryCloneService> logger)
    {
        _logger = logger;
        _cloneRoot = configuration["Repositories:CloneRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "repo-clones");
        Directory.CreateDirectory(_cloneRoot);
    }

    public async Task<GitCloneResult> CloneAsync(
        string repositoryUrl,
        string? branch = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return new GitCloneResult(false, null, "Repository URL is required.");
        }

        var cloneUrl = InjectToken(repositoryUrl.Trim(), accessToken);
        var targetPath = Path.Combine(_cloneRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetPath);

        var arguments = new StringBuilder("clone --depth 1 ");
        if (!string.IsNullOrWhiteSpace(branch))
        {
            arguments.Append(CultureInvariant($"--branch {branch.Trim()} "));
        }

        arguments.Append(CultureInvariant($"\"{cloneUrl}\" \"{targetPath}\""));

        try
        {
            var exitCode = await RunGitAsync(arguments.ToString(), cancellationToken);
            if (exitCode != 0)
            {
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, recursive: true);
                }

                return new GitCloneResult(false, null, "Git clone failed. Verify the URL, branch, and credentials.");
            }

            _logger.LogInformation("Cloned repository to {Path}", targetPath);
            return new GitCloneResult(true, targetPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Git clone failed for {Url}", repositoryUrl);
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, recursive: true);
            }

            return new GitCloneResult(false, null, ex.Message);
        }
    }

    internal static string InjectToken(string repositoryUrl, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)
            || !Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            return repositoryUrl;
        }

        var builder = new UriBuilder(uri)
        {
            UserName = accessToken,
            Password = string.Empty
        };
        return builder.Uri.ToString();
    }

    private static async Task<int> RunGitAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static string CultureInvariant(string value) => value;
}
