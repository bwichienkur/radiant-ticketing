using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class EfEntityTableMapperTests : IDisposable
{
    private readonly string _rootPath;
    private readonly EfEntityTableMapper _sut = new();

    public EfEntityTableMapperTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), $"eh-efmap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        WriteSampleRepository();
    }

    [Fact]
    public async Task MapEntitiesAsync_DetectsTableAttributeAndDbSetConvention()
    {
        var mappings = await _sut.MapEntitiesAsync(
            _rootPath,
            ["SampleApp.Data.AppDbContext"]);

        mappings.Should().Contain(m =>
            m.EntityClassName == "Customer"
            && m.TableName == "Customers"
            && m.MappingSource == Domain.Enums.EntityMappingSource.Attribute);

        mappings.Should().Contain(m =>
            m.EntityClassName == "Order"
            && m.TableName == "Orders");

        mappings.First(m => m.EntityClassName == "Customer").Properties.Should().Contain(p => p.PropertyName == "Id" && p.IsPrimaryKey);
        mappings.First(m => m.EntityClassName == "Customer").Properties.Should().Contain(p => p.PropertyName == "Email");
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
        Directory.CreateDirectory(dataDir);

        File.WriteAllText(Path.Combine(dataDir, "AppDbContext.cs"), """
            using Microsoft.EntityFrameworkCore;

            namespace SampleApp.Data;

            public class AppDbContext : DbContext
            {
                public DbSet<Customer> Customers => Set<Customer>();
                public DbSet<Order> Orders => Set<Order>();
            }
            """);

        File.WriteAllText(Path.Combine(dataDir, "Customer.cs"), """
            using System.ComponentModel.DataAnnotations.Schema;

            namespace SampleApp.Data;

            [Table("Customers", Schema = "sales")]
            public class Customer
            {
                public int Id { get; set; }
                public string Email { get; set; } = string.Empty;
            }
            """);

        File.WriteAllText(Path.Combine(dataDir, "Order.cs"), """
            namespace SampleApp.Data;

            public class Order
            {
                public int Id { get; set; }
            }
            """);
    }
}
