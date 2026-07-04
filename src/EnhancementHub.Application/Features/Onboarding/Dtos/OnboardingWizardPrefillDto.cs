namespace EnhancementHub.Application.Features.Onboarding.Dtos;

public sealed record OnboardingWizardPrefillDto(
    OnboardingStep1PrefillDto? Step1,
    OnboardingStep2PrefillDto? Step2,
    OnboardingStep3PrefillDto? Step3);

public sealed record OnboardingStep1PrefillDto(
    string Name,
    string? BusinessDomain,
    string? Purpose,
    string? RiskSensitiveAreas,
    string? OwnerTeamName);

public sealed record OnboardingStep2PrefillDto(
    string RepositoryName,
    string RepositoryPath,
    string DefaultBranch);

public sealed record OnboardingStep3PrefillDto(
    string ConnectionName,
    Domain.Enums.DatabaseProviderType Provider,
    bool IsReadOnly);
