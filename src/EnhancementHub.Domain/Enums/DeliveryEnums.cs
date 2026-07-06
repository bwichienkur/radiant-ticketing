namespace EnhancementHub.Domain.Enums;

public enum DeploymentEnvironmentType
{
    Test = 0,
    Uat = 1,
    Staging = 2,
    Production = 3
}

public enum CicdProvider
{
    Manual = 0,
    GitHubActions = 1,
    AzureDevOps = 2,
    Jenkins = 3,
    Webhook = 4
}

public enum DeploymentMechanism
{
    Custom = 0,
    AppService = 1,
    Kubernetes = 2,
    AzureFunctions = 3,
    VmIis = 4,
    StaticWebApp = 5
}

public enum DatabaseMigrationStrategy
{
    None = 0,
    EfMigrations = 1,
    Flyway = 2,
    Manual = 3
}

public enum DeliveryRunPhase
{
    Pending,
    Implementing,
    AwaitingPullRequestReview,
    DeployingToTest,
    RunningQa,
    AwaitingUat,
    UatApproved,
    ProdScheduled,
    DeployingToProduction,
    Completed,
    Failed
}
