using System.Data.Common;
using System.Net.Http.Json;
using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Models;
using EnhancementHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace EnhancementHub.Tests.Common;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "dev-secret-change-in-production-min-32-chars!!";

    private SqliteConnection? _connection;
    private bool _databaseInitialized;

    protected virtual IReadOnlyDictionary<string, string?>? AdditionalSettings => null;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", JwtSecret);
        builder.UseSetting("Jwt:Issuer", "EnhancementHub");
        builder.UseSetting("Jwt:Audience", "EnhancementHub");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Data Source=:memory:",
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = "EnhancementHub",
                ["Jwt:Audience"] = "EnhancementHub",
                ["Jwt:ExpiryMinutes"] = "60"
            };

            if (AdditionalSettings is not null)
            {
                foreach (var (key, value) in AdditionalSettings)
                {
                    settings[key] = value;
                }
            }

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EnhancementHubDbContext>>();
            services.RemoveAll<EnhancementHubDbContext>();
            services.RemoveAll<IEnhancementHubDbContext>();
            services.RemoveAll<DbContextOptions<ReportingDbContext>>();
            services.RemoveAll<ReportingDbContext>();
            services.RemoveAll<IReportingDbContext>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);
            services.AddDbContext<EnhancementHubDbContext>((sp, options) =>
            {
                var connection = sp.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
            services.AddDbContext<ReportingDbContext>((sp, options) =>
            {
                var connection = sp.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
            services.AddScoped<IEnhancementHubDbContext>(sp => sp.GetRequiredService<EnhancementHubDbContext>());
            services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ReportingDbContext>());

            CustomizeServices(services);
        });
    }

    protected virtual void CustomizeServices(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            options.TokenValidationParameters.ValidIssuer = "EnhancementHub";
            options.TokenValidationParameters.ValidAudience = "EnhancementHub";
        });
    }

    public async Task EnsureDatabaseInitializedAsync()
    {
        if (_databaseInitialized)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
        await db.Database.MigrateAsync();
        _databaseInitialized = true;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        Domain.Entities.User user,
        string password = "password123")
    {
        await EnsureDatabaseInitializedAsync();

        var client = CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password
        });

        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    public HttpClient CreateAuthenticatedClient(Domain.Entities.User user) =>
        CreateAuthenticatedClientAsync(user).GetAwaiter().GetResult();

    public TestDataBuilder CreateDataBuilder() => new(this);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
