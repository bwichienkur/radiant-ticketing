using EnhancementHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure;

public sealed class EnhancementHubDbContextFactory : IDesignTimeDbContextFactory<EnhancementHubDbContext>
{
    public EnhancementHubDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("../EnhancementHub.Api/appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=enhancementhub.db";
        var optionsBuilder = new DbContextOptionsBuilder<EnhancementHubDbContext>();

        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        return new EnhancementHubDbContext(optionsBuilder.Options);
    }
}
