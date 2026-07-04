using System.Net.Http.Json;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("ENHANCEMENTHUB_")
    .AddCommandLine(args)
    .Build();

var apiBaseUrl = configuration["Agent:ApiBaseUrl"]
    ?? throw new InvalidOperationException("Agent:ApiBaseUrl is required.");
var agentId = Guid.Parse(configuration["Agent:AgentId"]
    ?? throw new InvalidOperationException("Agent:AgentId is required."));
var connectionId = Guid.Parse(configuration["Agent:ConnectionId"]
    ?? throw new InvalidOperationException("Agent:ConnectionId is required."));
var connectionString = configuration["Agent:ConnectionString"]
    ?? throw new InvalidOperationException("Agent:ConnectionString is required.");
var provider = Enum.Parse<DatabaseProviderType>(
    configuration["Agent:Provider"] ?? nameof(DatabaseProviderType.Sqlite),
    ignoreCase: true);

Console.WriteLine($"EnhancementHub on-prem agent scanning {provider} database...");

var scanner = new DatabaseSchemaScanner(
    new DatabaseSchemaScannerFactory(
        new SqlServerSchemaScanner(),
        new PostgreSqlSchemaScanner()));

var scanResult = await scanner.ScanAsync(connectionString, provider);
Console.WriteLine($"Scanned {scanResult.Tables.Count} tables. Uploading to {apiBaseUrl}...");

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
var payload = new ScanPayload(connectionId, scanResult);
var response = await http.PostAsJsonAsync($"api/on-prem-agent/{agentId}/scan-results", payload);

if (!response.IsSuccessStatusCode)
{
    var body = await response.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"Upload failed ({(int)response.StatusCode}): {body}");
    return 1;
}

Console.WriteLine("Scan results uploaded successfully.");
return 0;

internal sealed record ScanPayload(Guid ConnectionId, DatabaseSchemaScanResult ScanResult);
