using System.IO.Compression;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class RepositoryArchiveExtractService : IRepositoryArchiveExtractService
{
    private readonly ILogger<RepositoryArchiveExtractService> _logger;
    private readonly string _extractRoot;
    private readonly long _maxBytes;

    public RepositoryArchiveExtractService(IConfiguration configuration, ILogger<RepositoryArchiveExtractService> logger)
    {
        _logger = logger;
        _extractRoot = configuration["Repositories:CloneRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "repo-clones");
        _maxBytes = configuration.GetValue("Repositories:ArchiveMaxSizeBytes", 500_000_000L);
        Directory.CreateDirectory(_extractRoot);
    }

    public async Task<RepositoryArchiveExtractResult> ExtractZipAsync(
        Stream archiveStream,
        CancellationToken cancellationToken = default)
    {
        if (archiveStream.CanSeek && archiveStream.Length > _maxBytes)
        {
            return new RepositoryArchiveExtractResult(false, null, $"Archive exceeds maximum size of {_maxBytes} bytes.");
        }

        var targetRoot = Path.Combine(_extractRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetRoot);

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        try
        {
            await using (var tempStream = File.Create(tempFile))
            {
                await archiveStream.CopyToAsync(tempStream, cancellationToken);
            }

            var fileInfo = new FileInfo(tempFile);
            if (fileInfo.Length > _maxBytes)
            {
                return new RepositoryArchiveExtractResult(false, null, $"Archive exceeds maximum size of {_maxBytes} bytes.");
            }

            using var archive = ZipFile.OpenRead(tempFile);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var destinationPath = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
                if (!destinationPath.StartsWith(Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    && destinationPath != Path.GetFullPath(targetRoot))
                {
                    throw new InvalidDataException("Archive entry escapes extraction directory.");
                }

                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                entry.ExtractToFile(destinationPath, overwrite: true);
            }

            var resolvedPath = ResolveRepositoryRoot(targetRoot);
            if (!Directory.Exists(resolvedPath))
            {
                return new RepositoryArchiveExtractResult(false, null, "Archive extracted but repository root could not be resolved.");
            }

            var hasSourceFiles = Directory
                .EnumerateFiles(resolvedPath, "*.*", SearchOption.AllDirectories)
                .Any(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".js", StringComparison.OrdinalIgnoreCase));

            if (!hasSourceFiles)
            {
                Directory.Delete(targetRoot, recursive: true);
                return new RepositoryArchiveExtractResult(false, null, "Archive does not contain recognizable source files.");
            }

            _logger.LogInformation("Extracted repository archive to {Path}", resolvedPath);
            return new RepositoryArchiveExtractResult(true, resolvedPath, null);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid ZIP archive uploaded.");
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, recursive: true);
            }

            return new RepositoryArchiveExtractResult(false, null, "Invalid ZIP archive.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract repository archive.");
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, recursive: true);
            }

            return new RepositoryArchiveExtractResult(false, null, ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    internal static string ResolveRepositoryRoot(string extractRoot)
    {
        var topLevel = Directory.GetDirectories(extractRoot)
            .Where(d => !Path.GetFileName(d).StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (topLevel.Count == 1)
        {
            return topLevel[0];
        }

        return extractRoot;
    }
}
