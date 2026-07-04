using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class RoslynRepositoryScannerTests : IDisposable
{
    private readonly string _rootPath;
    private readonly RoslynRepositoryScanner _sut = new(new EfEntityTableMapper());

    public RoslynRepositoryScannerTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), $"eh-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        WriteSampleRepository();
    }

    [Fact]
    public async Task ScanAsync_FindsControllersDbContextAndServices()
    {
        var result = await _sut.ScanAsync(_rootPath);

        result.TotalFilesScanned.Should().Be(3);
        result.DbContextTypes.Should().Contain("SampleApp.Data.AppDbContext");
        result.Controllers.Should().ContainSingle(c => c.Name == "OrdersController");
        result.Controllers.Single().Actions.Should().Contain("Get");
        result.Classes.Should().Contain(c => c.Name == "OrderService");
        result.Namespaces.Select(n => n.Name).Should().Contain("SampleApp.Services");
    }

    [Fact]
    public async Task ScanAsync_WhenDirectoryMissing_Throws()
    {
        var missingPath = Path.Combine(_rootPath, "missing");

        var act = () => _sut.ScanAsync(missingPath);

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private void WriteSampleRepository()
    {
        var dataDir = Path.Combine(_rootPath, "Data");
        var apiDir = Path.Combine(_rootPath, "Api");
        var servicesDir = Path.Combine(_rootPath, "Services");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(apiDir);
        Directory.CreateDirectory(servicesDir);

        File.WriteAllText(Path.Combine(dataDir, "AppDbContext.cs"), """
            using Microsoft.EntityFrameworkCore;

            namespace SampleApp.Data;

            public class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
            }
            """);

        File.WriteAllText(Path.Combine(apiDir, "OrdersController.cs"), """
            using Microsoft.AspNetCore.Mvc;

            namespace SampleApp.Api;

            [ApiController]
            [Route("api/orders")]
            public class OrdersController : ControllerBase
            {
                public IActionResult Get() => Ok();
            }
            """);

        File.WriteAllText(Path.Combine(servicesDir, "OrderService.cs"), """
            namespace SampleApp.Services;

            public class OrderService
            {
                public string PlaceOrder() => "ok";
            }
            """);
    }
}
