namespace EnhancementHub.Infrastructure.Options;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenAI";
    public bool PiiRedactionEnabled { get; set; } = true;
    public AiBudgetOptions Budget { get; set; } = new();
    public AiProviderOptions OpenAI { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();
}

public sealed class AiBudgetOptions
{
    public bool Enabled { get; set; } = true;
    public int DailyTokenLimit { get; set; } = 500_000;
    public decimal DailyCostLimitUsd { get; set; } = 50m;
}

public sealed class AiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public AiModelOptions Models { get; set; } = new();
}

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";
    public AiModelOptions Deployments { get; set; } = new();
}

public sealed class AiModelOptions
{
    public string EnhancementAnalysis { get; set; } = "gpt-4o-mini";
    public string RefactorPlan { get; set; } = "gpt-4o-mini";
}
