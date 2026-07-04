using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class AzureDevOpsTicketExporter : IExternalTicketExporter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureDevOpsTicketExporter> _logger;

    public AzureDevOpsTicketExporter(HttpClient httpClient, IConfiguration configuration, ILogger<AzureDevOpsTicketExporter> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public ExternalTicketProvider Provider => ExternalTicketProvider.AzureDevOps;

    public async Task<ExternalTicketExportResult> ExportAsync(
        ExternalTicketExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var organization = _configuration["ExternalTickets:AzureDevOps:Organization"];
        var project = _configuration["ExternalTickets:AzureDevOps:Project"];
        var pat = _configuration["ExternalTickets:AzureDevOps:Pat"];

        if (string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(pat))
        {
            return new ExternalTicketExportResult(false, null, null, "Azure DevOps export is not configured.");
        }

        var payload = new object[]
        {
            new { op = "add", path = "/fields/System.Title", value = (object)request.Title },
            new { op = "add", path = "/fields/System.Description", value = (object)BuildDescription(request) },
            new { op = "add", path = "/fields/Microsoft.VSTS.Common.Priority", value = (object)MapPriority(request.Priority) }
        };

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/$Issue?api-version=7.1");
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        message.Headers.Accept.ParseAdd("application/json");
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json-patch+json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure DevOps work item creation failed: {Content}", content);
            return new ExternalTicketExportResult(false, null, null, $"Azure DevOps API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(content);
        var id = doc.RootElement.GetProperty("id").GetInt32().ToString();
        var url = doc.RootElement.GetProperty("_links").GetProperty("html").GetProperty("href").GetString();
        return new ExternalTicketExportResult(true, id, url, null);
    }

    private static int MapPriority(string priority)
    {
        return priority.ToLowerInvariant() switch
        {
            "low" => 4,
            "medium" => 3,
            "high" => 2,
            "critical" => 1,
            _ => 3
        };
    }

    private static string BuildDescription(ExternalTicketExportRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.Description);
        if (!string.IsNullOrWhiteSpace(request.AnalysisSummary))
        {
            sb.AppendLine("<br/><br/><b>AI Analysis</b><br/>");
            sb.AppendLine(request.AnalysisSummary);
        }

        sb.AppendLine($"<br/><br/><i>EnhancementHub request {request.EnhancementRequestId}</i>");
        return sb.ToString();
    }
}
