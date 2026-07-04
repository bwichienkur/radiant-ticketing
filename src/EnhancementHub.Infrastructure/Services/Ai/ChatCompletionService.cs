using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.DependencyInjection;
using EnhancementHub.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Ai;

public sealed class ChatCompletionService : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AiOptions _options;
    private readonly IPiiRedactionService _piiRedaction;
    private readonly IAiUsageBudgetService _budgetService;
    private readonly ILogger<ChatCompletionService> _logger;

    public ChatCompletionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<AiOptions> options,
        IPiiRedactionService piiRedaction,
        IAiUsageBudgetService budgetService,
        ILogger<ChatCompletionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _options = options.Value;
        _piiRedaction = piiRedaction;
        _budgetService = budgetService;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ResolveApiKey()) || !string.IsNullOrWhiteSpace(LegacyApiKey());

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = LegacyApiKey();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ChatCompletionResponse
            {
                Content = string.Empty,
                ModelUsed = "not-configured",
                IsMock = true
            };
        }

        await _budgetService.EnsureWithinBudgetAsync(cancellationToken);

        var model = ResolveModel(request.WorkflowStep);
        var userPrompt = _piiRedaction.Redact(request.UserPrompt);
        var systemPrompt = _piiRedaction.Redact(request.SystemPrompt);

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        if (request.JsonResponse)
        {
            requestBody["response_format"] = new { type = "json_object" };
        }

        var (url, useAzureAuth) = BuildEndpoint(model);
        var client = _httpClientFactory.CreateClient(InfrastructureServiceExtensions.OpenAiHttpClientName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        if (useAzureAuth)
        {
            httpRequest.Headers.Add("api-key", apiKey);
        }
        else
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Chat completion failed ({Status}): {Body}", response.StatusCode, responseJson);
            throw new InvalidOperationException($"AI provider returned {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var promptTokens = 0;
        var completionTokens = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            completionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
        }

        var totalTokens = promptTokens + completionTokens;
        var cost = AiCostEstimator.Estimate(model, promptTokens, completionTokens);

        return new ChatCompletionResponse
        {
            Content = content,
            ModelUsed = model,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            EstimatedCostUsd = cost,
            IsMock = false
        };
    }

    private string ResolveApiKey()
    {
        if (IsAzureProvider())
        {
            return _options.AzureOpenAI.ApiKey;
        }

        return !string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey)
            ? _options.OpenAI.ApiKey
            : LegacyApiKey();
    }

    private string LegacyApiKey() => _configuration["OpenAI:ApiKey"] ?? string.Empty;

    private bool IsAzureProvider() =>
        _options.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase)
        || _options.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase);

    private string ResolveModel(AiWorkflowStep step) =>
        step switch
        {
            AiWorkflowStep.RefactorPlan => IsAzureProvider()
                ? _options.AzureOpenAI.Deployments.RefactorPlan
                : ResolveOpenAiModel(_options.OpenAI.Models.RefactorPlan),
            _ => IsAzureProvider()
                ? _options.AzureOpenAI.Deployments.EnhancementAnalysis
                : ResolveOpenAiModel(_options.OpenAI.Models.EnhancementAnalysis)
        };

    private string ResolveOpenAiModel(string configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? _configuration["OpenAI:Model"] ?? "gpt-4o-mini"
            : configured;

    private (string Url, bool UseAzureAuth) BuildEndpoint(string model)
    {
        if (IsAzureProvider())
        {
            var endpoint = _options.AzureOpenAI.Endpoint.TrimEnd('/');
            var apiVersion = _options.AzureOpenAI.ApiVersion;
            var url = $"{endpoint}/openai/deployments/{model}/chat/completions?api-version={apiVersion}";
            return (url, true);
        }

        var baseUrl = !string.IsNullOrWhiteSpace(_options.OpenAI.BaseUrl)
            ? _options.OpenAI.BaseUrl
            : _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";

        return ($"{baseUrl.TrimEnd('/')}/chat/completions", false);
    }
}
