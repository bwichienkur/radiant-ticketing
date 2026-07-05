using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class ServiceNowTicketExporter : IExternalTicketExporter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceNowTicketExporter> _logger;

    public ServiceNowTicketExporter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ServiceNowTicketExporter> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public ExternalTicketProvider Provider => ExternalTicketProvider.ServiceNow;

    public async Task<ExternalTicketExportResult> ExportAsync(
        ExternalTicketExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var instanceUrl = _configuration["Integrations:ServiceNow:InstanceUrl"];
        var username = _configuration["Integrations:ServiceNow:Username"];
        var password = _configuration["Integrations:ServiceNow:Password"];
        var table = _configuration["Integrations:ServiceNow:TableName"] ?? "change_request";

        if (string.IsNullOrWhiteSpace(instanceUrl)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            return new ExternalTicketExportResult(false, null, null, "ServiceNow export is not configured.");
        }

        var payload = new
        {
            short_description = request.Title,
            description = BuildDescription(request),
            priority = MapPriority(request.Priority),
            u_enhancement_request_id = request.EnhancementRequestId.ToString()
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{instanceUrl.TrimEnd('/')}/api/now/table/{table}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ServiceNow export failed: {Status} {Body}", response.StatusCode, body);
            return new ExternalTicketExportResult(false, null, null, body);
        }

        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        var sysId = result.GetProperty("sys_id").GetString();
        var number = result.TryGetProperty("number", out var num) ? num.GetString() : sysId;
        var url = $"{instanceUrl.TrimEnd('/')}/nav_to.do?uri={table}.do?sys_id={sysId}";

        return new ExternalTicketExportResult(true, number, url, null);
    }

    private static string BuildDescription(ExternalTicketExportRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.Description);
        if (!string.IsNullOrWhiteSpace(request.AnalysisSummary))
        {
            sb.AppendLine();
            sb.AppendLine("AI analysis:");
            sb.AppendLine(request.AnalysisSummary);
        }

        return sb.ToString();
    }

    private static string MapPriority(string priority) =>
        priority.ToLowerInvariant() switch
        {
            "critical" or "high" => "2",
            "low" => "4",
            _ => "3"
        };
}
