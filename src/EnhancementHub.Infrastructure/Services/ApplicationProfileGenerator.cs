using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ApplicationProfileGenerator
{
    private readonly IGitRepositoryScanner _scanner;
    private readonly IEnhancementHubDbContext _dbContext;

    public ApplicationProfileGenerator(IGitRepositoryScanner scanner, IEnhancementHubDbContext dbContext)
    {
        _scanner = scanner;
        _dbContext = dbContext;
    }

    public async Task<ApplicationProfile> GenerateAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var repository = await _dbContext.Repositories
            .Include(r => r.Application)
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Repository {repositoryId} not found.");

        var rootPath = ResolveRepositoryPath(repository);
        var scan = await _scanner.ScanAsync(rootPath, cancellationToken);

        var keyComponents = JsonSerializer.Serialize(scan.Classes
            .OrderByDescending(c => c.Methods.Count)
            .Take(20)
            .Select(c => new { c.Namespace, c.Name, Methods = c.Methods.Count }));

        var existing = await _dbContext.ApplicationProfiles
            .FirstOrDefaultAsync(p => p.RepositoryId == repositoryId && p.ApplicationId == repository.ApplicationId, cancellationToken);

        if (existing is not null)
        {
            existing.Purpose = repository.Application?.Purpose ?? repository.Application?.Description;
            existing.BusinessDomain = repository.Application?.BusinessDomain;
            existing.KeyComponents = keyComponents;
            existing.DatabaseUsage = string.Join(", ", scan.DbContextTypes);
            existing.ExternalIntegrations = string.Join(", ", scan.Controllers.Select(c => c.Name));
            existing.InternalDependencies = string.Join(", ", scan.Namespaces.Select(n => n.Name).Distinct().Take(30));
            existing.RiskSensitiveAreas = repository.Application?.RiskSensitiveAreas;
            if (!string.IsNullOrWhiteSpace(repository.Application?.DeploymentNotes))
            {
                existing.DeploymentNotes = repository.Application.DeploymentNotes;
            }
            existing.GeneratedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var profile = new ApplicationProfile
        {
            Id = Guid.NewGuid(),
            ApplicationId = repository.ApplicationId,
            RepositoryId = repositoryId,
            Purpose = repository.Application?.Purpose ?? repository.Application?.Description,
            BusinessDomain = repository.Application?.BusinessDomain,
            KeyComponents = keyComponents,
            DatabaseUsage = string.Join(", ", scan.DbContextTypes),
            ExternalIntegrations = string.Join(", ", scan.Controllers.Select(c => c.Name)),
            InternalDependencies = string.Join(", ", scan.Namespaces.Select(n => n.Name).Distinct().Take(30)),
            RiskSensitiveAreas = repository.Application?.RiskSensitiveAreas,
            DeploymentNotes = repository.Application?.DeploymentNotes,
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ApplicationProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static string ResolveRepositoryPath(Repository repository)
    {
        if (Directory.Exists(repository.Url))
        {
            return repository.Url;
        }

        throw new DirectoryNotFoundException(
            $"Repository path '{repository.Url}' is not accessible. Clone the repository locally and update Repository.Url.");
    }
}
