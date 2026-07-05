namespace EnhancementHub.Application.Options;

public sealed class IntegrationsOptions
{
    public const string SectionName = "Integrations";

    public GitHubWebhookOptions GitHub { get; set; } = new();

    public SlackIntakeOptions Slack { get; set; } = new();

    public TeamsIntakeOptions Teams { get; set; } = new();

    public PolyglotOptions Polyglot { get; set; } = new();

    public ServiceNowOptions ServiceNow { get; set; } = new();
}

public sealed class GitHubWebhookOptions
{
    public bool Enabled { get; set; }

    public string? WebhookSecret { get; set; }
}

public sealed class SlackIntakeOptions
{
    public bool Enabled { get; set; }

    public string? SigningSecret { get; set; }

    public string DefaultPriority { get; set; } = "Medium";
}

public sealed class TeamsIntakeOptions
{
    public bool Enabled { get; set; }

    public string? IntakeSecret { get; set; }

    public string DefaultPriority { get; set; } = "Medium";
}

public sealed class PolyglotOptions
{
    public bool Enabled { get; set; } = true;

    public string[] SupportedLanguages { get; set; } = ["java", "python", "typescript", "javascript"];
}

public sealed class ServiceNowOptions
{
    public bool Enabled { get; set; }

    public string? InstanceUrl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string TableName { get; set; } = "change_request";

    public string? WebhookSecret { get; set; }
}
