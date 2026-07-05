using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.DependencyInjection;
using EnhancementHub.Infrastructure.DependencyInjection;
using EnhancementHub.Infrastructure.Middleware;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services.Notifications;
using EnhancementHub.Web.Hubs;
using EnhancementHub.Web.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEnhancementHubOpenTelemetry(builder.Configuration, "EnhancementHub.Web");
builder.Services.AddEnhancementHubHealthChecks();
builder.Services.AddSignalR();
builder.Services.RemoveAll<IRequestCollaborationNotifier>();
builder.Services.AddScoped<IRequestCollaborationNotifier, RequestCollaborationNotifier>();
builder.Services.AddScoped<SignalRNotificationPublisher>();
builder.Services.AddScoped<INotificationPublisher>(sp => new CompositeNotificationPublisher(
    [
        sp.GetRequiredService<SignalRNotificationPublisher>(),
        sp.GetRequiredService<EmailNotificationPublisher>(),
        sp.GetRequiredService<TeamsWebhookNotificationPublisher>()
    ],
    sp.GetRequiredService<ILogger<CompositeNotificationPublisher>>()));
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Signup");
});
builder.Services.AddScoped<EnhancementHub.Web.Services.DevAuthService>();

builder.Services.AddEnhancementHubCookieAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();
    await db.Database.MigrateAsync();

    var seederDb = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder");
    await DevDataSeeder.SeedAsync(seederDb, hasher, logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseTenantIsolation();
app.UseAuthorization();
app.MapEnhancementHubHealthChecks();
app.MapEnhancementHubObservabilityEndpoints(builder.Configuration);
app.MapControllers();
app.MapRazorPages();
app.MapHub<PlatformNotificationHub>("/hubs/notifications");
app.MapHub<EnhancementRequestCollaborationHub>("/hubs/request-collaboration");

app.Run();
