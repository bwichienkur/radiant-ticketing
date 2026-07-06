using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var repositoryTarget = 200;
var applicationBatchSize = 50;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] is "--repositories" or "-r" && i + 1 < args.Length && int.TryParse(args[i + 1], out var count))
    {
        repositoryTarget = Math.Clamp(count, 1, 2000);
    }
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Default")
            ?? "Data Source=enhancementhub.db";

        services.AddDbContext<EnhancementHubDbContext>(options =>
            options.UseSqlite(connectionString));
    })
    .Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
await db.Database.MigrateAsync();

var existingRepositories = await db.Repositories.CountAsync();
if (existingRepositories >= repositoryTarget)
{
    Console.WriteLine($"Already have {existingRepositories} repositories (target {repositoryTarget}). Nothing to seed.");
    return 0;
}

var admin = await db.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Role)
    .FirstOrDefaultAsync();

if (admin is null)
{
    Console.Error.WriteLine("No active users found. Start the app once to seed demo data.");
    return 1;
}

var teamId = await db.Teams.AsNoTracking().Select(t => t.Id).FirstOrDefaultAsync();
if (teamId == Guid.Empty)
{
    teamId = Guid.NewGuid();
    db.Teams.Add(new Team
    {
        Id = teamId,
        Name = "Load Test Team",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = admin.Id
    });
    await db.SaveChangesAsync();
}

var toCreate = repositoryTarget - existingRepositories;
Console.WriteLine($"Seeding {toCreate} synthetic repositories for load testing...");

var now = DateTime.UtcNow;
var created = 0;

while (created < toCreate)
{
    var batchCount = Math.Min(applicationBatchSize, toCreate - created);
    var applications = new List<Application>(batchCount);
    var repositories = new List<Repository>(batchCount);

    for (var i = 0; i < batchCount; i++)
    {
        var index = existingRepositories + created + i + 1;
        var applicationId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        applications.Add(new Application
        {
            Id = applicationId,
            Name = $"LoadTest App {index:D4}",
            BusinessDomain = "LoadTest",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = admin.Id
        });

        repositories.Add(new Repository
        {
            Id = repositoryId,
            ApplicationId = applicationId,
            Name = $"loadtest-repo-{index:D4}",
            Url = $"https://example.com/loadtest/{index:D4}",
            DefaultBranch = "main",
            IndexingStatus = IndexingStatus.Completed,
            LastIndexedAt = now.AddHours(-1),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = admin.Id
        });
    }

    db.Applications.AddRange(applications);
    db.Repositories.AddRange(repositories);
    await db.SaveChangesAsync();
    created += batchCount;
    Console.WriteLine($"  {created}/{toCreate} repositories created...");
}

Console.WriteLine($"Load test seed complete. Total repositories: {await db.Repositories.CountAsync()}");
return 0;
