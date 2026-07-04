using System.Text;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Application.Features.Onboarding.Queries;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Onboarding;

[Authorize]
public class WizardModel : PageModel
{
    private readonly IMediator _mediator;

    public WizardModel(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid SessionId { get; set; }

    [BindProperty]
    public Step1Input Step1 { get; set; } = new();

    [BindProperty]
    public Step2Input Step2 { get; set; } = new();

    [BindProperty]
    public Step2GitInput Step2Git { get; set; } = new();

    [BindProperty]
    public Step2GitHubAppInput Step2GitHubApp { get; set; } = new();

    [BindProperty]
    public Step3BuilderInput Step3Builder { get; set; } = new();

    [BindProperty]
    public Step3Input Step3 { get; set; } = new();

    public OnboardingSessionDto Session { get; private set; } = null!;
    public RepositoryPathValidationDto? PathValidation { get; private set; }
    public ApplicationDiscoveryResultDto? DiscoveryResult { get; private set; }
    public OnboardingReviewDto? Review { get; private set; }
    public OnPremAgentSetupDto? OnPremSetup { get; private set; }
    public GitHubAppStatusDto? GitHubAppStatus { get; private set; }
    public string? BuiltConnectionString { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public static IReadOnlyList<StepDefinition> StepDefinitions =>
    [
        new(1, "Basics"),
        new(2, "Code"),
        new(3, "Database"),
        new(4, "Discovery"),
        new(5, "Review"),
        new(6, "Done")
    ];

    public async Task<IActionResult> OnGetAsync(int? step, CancellationToken cancellationToken)
    {
        if (Id == Guid.Empty)
        {
            var started = await _mediator.Send(new StartOnboardingSessionCommand(), cancellationToken);
            return RedirectToPage(new { id = started.Id });
        }

        Session = await _mediator.Send(new GetOnboardingSessionQuery(Id), cancellationToken);
        SessionId = Session.Id;

        if (step.HasValue && step.Value >= 1 && step.Value <= 6)
        {
            var targetStep = (OnboardingStep)step.Value;
            if ((int)targetStep < (int)Session.CurrentStep)
            {
                Session = await _mediator.Send(
                    new AdvanceOnboardingSessionCommand(Session.Id, targetStep, Session.ApplicationId, Session.SkipDatabase),
                    cancellationToken);
            }
        }

        await LoadStepContextAsync(cancellationToken);
        PrefillForms();

        if (Session.CurrentStep == OnboardingStep.ConnectCode)
        {
            GitHubAppStatus = await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken);
        }

        if (Session.CurrentStep == OnboardingStep.RunDiscovery
            && Session.DiscoveryJobState == DiscoveryJobState.Completed)
        {
            return RedirectToPage(new { id = SessionId, step = 5 });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStep1Async(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(Step1.Name))
        {
            ErrorMessage = "Application name is required.";
            return Page();
        }

        var application = await _mediator.Send(new CreateApplicationCommand(
            Step1.Name,
            Step1.BusinessDomain,
            Step1.Purpose,
            null,
            Step1.RiskSensitiveAreas,
            Step1.OwnerTeamName), cancellationToken);

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.ConnectCode,
            application.Id), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostValidatePathAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);
        PathValidation = await _mediator.Send(new ValidateRepositoryPathQuery(Step2.RepositoryPath), cancellationToken);
        PrefillForms();
        return Page();
    }

    public async Task<IActionResult> OnPostStep2Async(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        if (string.IsNullOrWhiteSpace(Step2.RepositoryName) || string.IsNullOrWhiteSpace(Step2.RepositoryPath))
        {
            ErrorMessage = "Repository name and path are required.";
            PrefillForms();
            return Page();
        }

        var validation = await _mediator.Send(new ValidateRepositoryPathQuery(Step2.RepositoryPath), cancellationToken);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.ErrorMessage ?? "Repository path is not accessible.";
            PathValidation = validation;
            PrefillForms();
            return Page();
        }

        await _mediator.Send(new RegisterRepositoryCommand(
            Session.ApplicationId.Value,
            Step2.RepositoryName.Trim(),
            Step2.RepositoryPath.Trim(),
            ExternalTicketProvider.GitHub,
            string.IsNullOrWhiteSpace(Step2.DefaultBranch) ? "main" : Step2.DefaultBranch.Trim()), cancellationToken);

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.ConnectDatabase,
            Session.ApplicationId), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostStep3Async(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        if (string.IsNullOrWhiteSpace(Step3.ConnectionName) || string.IsNullOrWhiteSpace(Step3.ConnectionString))
        {
            ErrorMessage = "Connection name and connection string are required.";
            PrefillForms();
            return Page();
        }

        await _mediator.Send(new RegisterDatabaseConnectionCommand(
            Session.ApplicationId.Value,
            Step3.ConnectionName.Trim(),
            Step3.Provider,
            Step3.ConnectionString.Trim(),
            Step3.IsReadOnly), cancellationToken);

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.RunDiscovery,
            Session.ApplicationId,
            false), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostSkipDatabaseAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.RunDiscovery,
            Session.ApplicationId,
            true), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostUploadZipAsync(IFormFile zipFile, CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        if (zipFile is null || zipFile.Length == 0)
        {
            ErrorMessage = "ZIP file is required.";
            GitHubAppStatus = await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken);
            PrefillForms();
            return Page();
        }

        if (!string.Equals(Path.GetExtension(zipFile.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only .zip archives are supported.";
            GitHubAppStatus = await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken);
            PrefillForms();
            return Page();
        }

        var repositoryName = string.IsNullOrWhiteSpace(Step2.RepositoryName)
            ? Path.GetFileNameWithoutExtension(zipFile.FileName)
            : Step2.RepositoryName.Trim();

        await using var stream = zipFile.OpenReadStream();
        var result = await _mediator.Send(new UploadRepositoryArchiveCommand(
            Session.ApplicationId.Value,
            repositoryName,
            stream), cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to extract repository archive.";
            GitHubAppStatus = await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken);
            PrefillForms();
            return Page();
        }

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.ConnectDatabase,
            Session.ApplicationId), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostCloneGitHubAppAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        var result = await _mediator.Send(new CloneGitHubAppRepositoryCommand(
            Session.ApplicationId.Value,
            Step2GitHubApp.RepositoryName,
            Step2GitHubApp.Owner,
            Step2GitHubApp.Repository,
            Step2GitHubApp.DefaultBranch,
            Step2GitHubApp.InstallationId), cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "GitHub App clone failed.";
            GitHubAppStatus = await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken);
            PrefillForms();
            return Page();
        }

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.ConnectDatabase,
            Session.ApplicationId), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostCloneGitAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        var result = await _mediator.Send(new CloneGitRepositoryCommand(
            Session.ApplicationId.Value,
            Step2Git.RepositoryName,
            Step2Git.RepositoryUrl,
            Step2Git.DefaultBranch,
            Step2Git.AccessToken), cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Git clone failed.";
            PrefillForms();
            return Page();
        }

        await _mediator.Send(new AdvanceOnboardingSessionCommand(
            SessionId,
            OnboardingStep.ConnectDatabase,
            Session.ApplicationId), cancellationToken);

        return RedirectToPage(new { id = SessionId });
    }

    public async Task<IActionResult> OnPostBuildConnectionStringAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);
        var built = await _mediator.Send(new BuildDatabaseConnectionStringQuery(
            Step3Builder.Provider,
            Step3Builder.Host,
            Step3Builder.Port,
            Step3Builder.Database,
            Step3Builder.Username,
            Step3Builder.Password,
            Step3Builder.IntegratedSecurity), cancellationToken);

        BuiltConnectionString = built.ConnectionString;
        Step3.ConnectionString = built.ConnectionString;
        Step3.Provider = Step3Builder.Provider;
        PrefillForms();
        return Page();
    }

    public async Task<IActionResult> OnPostSetupOnPremAgentAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        OnPremSetup = await _mediator.Send(new SetupOnPremAgentForOnboardingCommand(
            SessionId,
            Session.ApplicationId.Value,
            Step3.ConnectionName,
            Step3.Provider), cancellationToken);

        SuccessMessage = "On-prem agent registered. Run the agent in your environment, then continue to discovery.";
        Session = await _mediator.Send(new GetOnboardingSessionQuery(SessionId), cancellationToken);
        PrefillForms();
        return Page();
    }

    public async Task<IActionResult> OnPostStep4Async(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        Session = await _mediator.Send(new QueueApplicationDiscoveryCommand(
            Session.ApplicationId.Value,
            SessionId), cancellationToken);

        SuccessMessage = "Discovery queued. This page will refresh while discovery runs.";
        PrefillForms();
        return Page();
    }

    public async Task<IActionResult> OnPostExportDocsAsync(CancellationToken cancellationToken)
    {
        await LoadSessionAsync(cancellationToken);

        if (!Session.ApplicationId.HasValue)
        {
            return RedirectToPage(new { id = SessionId, step = 1 });
        }

        var export = await _mediator.Send(
            new ExportDocumentationCommand(Session.ApplicationId.Value, DocumentationExportFormat.Both),
            cancellationToken);

        return File(Encoding.UTF8.GetBytes(export.Content), export.ContentType, export.FileName);
    }

    public async Task<IActionResult> OnPostCompleteAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new CompleteOnboardingSessionCommand(SessionId), cancellationToken);
        return RedirectToPage(new { id = SessionId, step = 6 });
    }

    private async Task LoadSessionAsync(CancellationToken cancellationToken)
    {
        SessionId = Id != Guid.Empty ? Id : SessionId;
        Session = await _mediator.Send(new GetOnboardingSessionQuery(SessionId), cancellationToken);
    }

    private async Task LoadStepContextAsync(CancellationToken cancellationToken)
    {
        if (Session.ApplicationId.HasValue
            && (Session.CurrentStep == OnboardingStep.ReviewExport || Session.CurrentStep == OnboardingStep.Complete))
        {
            Review = await _mediator.Send(new GetOnboardingReviewQuery(Session.ApplicationId.Value), cancellationToken);
        }
    }

    private void PrefillForms()
    {
        if (!string.IsNullOrWhiteSpace(Session.ApplicationName))
        {
            Step1.Name ??= Session.ApplicationName;
        }

        if (string.IsNullOrWhiteSpace(Step2.DefaultBranch))
        {
            Step2.DefaultBranch = "main";
        }

        if (string.IsNullOrWhiteSpace(Step3.ConnectionName))
        {
            Step3.ConnectionName = $"{Session.ApplicationName ?? "App"} Database";
            Step3.ConnectionString = "Data Source=enhancementhub.db";
            Step3.IsReadOnly = true;
        }
    }

    public sealed record StepDefinition(int Number, string Label);

    public sealed class Step1Input
    {
        public string Name { get; set; } = string.Empty;
        public string? BusinessDomain { get; set; }
        public string? Purpose { get; set; }
        public string? RiskSensitiveAreas { get; set; }
        public string? OwnerTeamName { get; set; }
    }

    public sealed class Step2Input
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string RepositoryPath { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = "main";
    }

    public sealed class Step2GitInput
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string RepositoryUrl { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = "main";
        public string? AccessToken { get; set; }
    }

    public sealed class Step2GitHubAppInput
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = "main";
        public long? InstallationId { get; set; }
    }

    public sealed class Step3Input
    {
        public string ConnectionName { get; set; } = string.Empty;
        public DatabaseProviderType Provider { get; set; } = DatabaseProviderType.Sqlite;
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; } = true;
    }

    public sealed class Step3BuilderInput
    {
        public DatabaseProviderType Provider { get; set; } = DatabaseProviderType.Sqlite;
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "enhancementhub";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool IntegratedSecurity { get; set; }
    }
}
