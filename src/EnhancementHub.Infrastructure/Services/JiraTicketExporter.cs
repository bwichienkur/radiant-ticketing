using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class JiraTicketExporter : IExternalTicketExporter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JiraTicketExporter> _logger;

    public JiraTicketExporter(HttpClient httpClient, IConfiguration configuration, ILogger<JiraTicketExporter> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public ExternalTicketProvider Provider => ExternalTicketProvider.Jira;

    public async Task<ExternalTicketExportResult> ExportAsync(
        ExternalTicketExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["ExternalTickets:Jira:BaseUrl"];
        var email = _configuration["ExternalTickets:Jira:Email"];
        var apiToken = _configuration["ExternalTickets:Jira:ApiToken"];
        var projectKey = _configuration["ExternalTickets:Jira:ProjectKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(projectKey))
        {
            return new ExternalTicketExportResult(false, null, null, "Jira export is not configured.");
        }

        var payload = new
        {
            fields = new
            {
                project = new { key = projectKey },
                summary = request.Title,
                description = BuildDescription(request),
                issuetype = new { name = "Task" },
                priority = new { name = MapPriority(request.Priority) }
            }
        };

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{apiToken}"));
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/rest/api/3/issue");
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        message.Headers.Accept.ParseAdd("application/json");
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Jira issue creation failed: {Content}", content);
            return new ExternalTicketExportResult(false, null, null, $"Jira API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(content);
        var key = doc.RootElement.GetProperty("key").GetString();
        var url = $"{baseUrl.TrimEnd('/')}/browse/{key}";
        return new ExternalTicketExportResult(true, key, url, null);
    }

    private static string MapPriority(string priority) => priority.ToLowerInvariant() switch
    {
        "low" => "Low",
        "medium" => "Medium",
        "high" => "High",
        "critical" => "Highest",
        _ => "Medium"
    };

    private static string BuildDescription(ExternalTicketExportRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.Description);
        if (!string.IsNullOrWhiteSpace(request.AnalysisSummary))
        {
            sb.AppendLine();
            sb.AppendLine("AI Analysis:");
            sb.AppendLine(request.AnalysisSummary);
        }

        sb.AppendLine();
        sb.AppendLine($"EnhancementHub request: {request.EnhancementRequestId}");
        return sb.ToString();
    }
}
