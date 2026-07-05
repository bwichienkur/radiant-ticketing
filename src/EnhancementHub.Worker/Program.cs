using EnhancementHub.Application.DependencyInjection;
using EnhancementHub.Infrastructure.DependencyInjection;
using Hangfire;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, registerBackgroundJobs: true);
    builder.Services.AddEnhancementHubOpenTelemetry(builder.Configuration, "EnhancementHub.Worker");
    builder.Services.AddEnhancementHubHealthChecks();

    var app = builder.Build();
    app.MapEnhancementHubHealthChecks();
    app.MapEnhancementHubObservabilityEndpoints(builder.Configuration);

    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire");
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
