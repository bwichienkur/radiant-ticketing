using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class GitHubTicketExporter : IExternalTicketExporter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubTicketExporter> _logger;

    public GitHubTicketExporter(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubTicketExporter> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public ExternalTicketProvider Provider => ExternalTicketProvider.GitHub;

    public async Task<ExternalTicketExportResult> ExportAsync(
        ExternalTicketExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = _configuration["ExternalTickets:GitHub:Token"];
        var owner = _configuration["ExternalTickets:GitHub:Owner"];
        var repo = _configuration["ExternalTickets:GitHub:Repository"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return new ExternalTicketExportResult(false, null, null, "GitHub export is not configured.");
        }

        var body = BuildBody(request);
        var payload = JsonSerializer.Serialize(new { title = request.Title, body, labels = new[] { "enhancement" } });

        using var message = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{owner}/{repo}/issues");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.UserAgent.ParseAdd("EnhancementHub/1.0");
        message.Headers.Accept.ParseAdd("application/vnd.github+json");
        message.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub issue creation failed: {Content}", content);
            return new ExternalTicketExportResult(false, null, null, $"GitHub API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(content);
        var number = doc.RootElement.GetProperty("number").GetInt32().ToString();
        var url = doc.RootElement.GetProperty("html_url").GetString();
        return new ExternalTicketExportResult(true, number, url, null);
    }

    private static string BuildBody(ExternalTicketExportRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.Description);
        sb.AppendLine();
        sb.AppendLine($"**Priority:** {request.Priority}");
        if (!string.IsNullOrWhiteSpace(request.AnalysisSummary))
        {
            sb.AppendLine();
            sb.AppendLine("## AI Analysis");
            sb.AppendLine(request.AnalysisSummary);
        }

        sb.AppendLine();
        sb.AppendLine($"_Exported from EnhancementHub request {request.EnhancementRequestId}_");
        return sb.ToString();
    }
}
