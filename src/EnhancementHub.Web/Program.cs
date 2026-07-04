using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.DependencyInjection;
using EnhancementHub.Infrastructure.DependencyInjection;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Web.Hubs;
using EnhancementHub.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});
builder.Services.AddScoped<EnhancementHub.Web.Services.DevAuthService>();

builder.Services.AddEnhancementHubCookieAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

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
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<PlatformNotificationHub>("/hubs/notifications");

app.Run();
