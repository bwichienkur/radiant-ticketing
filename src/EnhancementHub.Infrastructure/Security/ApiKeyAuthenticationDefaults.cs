namespace EnhancementHub.Infrastructure.Security;

public static class ApiKeyAuthenticationDefaults
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string KeyPrefix = "eh_";
}
