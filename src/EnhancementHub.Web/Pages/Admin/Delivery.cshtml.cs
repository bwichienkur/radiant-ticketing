using EnhancementHub.Application.Features.Delivery.Commands;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Application.Features.Delivery.Queries;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor delivery page — redirects to <c>/Spa/Admin/Delivery</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Admin/Delivery. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class DeliveryModel : PageModel
{
    private readonly IMediator _mediator;

    public DeliveryModel(IMediator mediator) => _mediator = mediator;

    public TenantDeliveryProfileDto? Profile { get; private set; }

    public DeliveryProfileValidationResultDto? Validation { get; private set; }

    public string? StatusMessage { get; private set; }

    [BindProperty]
    public CicdProvider DefaultCicdProvider { get; set; } = CicdProvider.GitHubActions;

    [BindProperty]
    public string? VaultSecretPrefix { get; set; }

    [BindProperty]
    public bool AutoImplementOnApprove { get; set; }

    [BindProperty]
    public bool AutoDeployToTest { get; set; }

    [BindProperty]
    public bool RequirePullRequestReview { get; set; } = true;

    [BindProperty]
    public bool RequireUatSignoff { get; set; } = true;

    [BindProperty]
    public bool RequireProdChangeWindow { get; set; } = true;

    [BindProperty]
    public string? ChangeWindowNotes { get; set; }

    [BindProperty]
    public int QaVideoRetentionDays { get; set; } = 90;

    [BindProperty]
    public bool AllowOneClickProdDeploy { get; set; } = true;

    [BindProperty]
    public bool AllowOneClickRollback { get; set; } = true;

    [BindProperty]
    public TestDataStrategy TestDataStrategy { get; set; } = TestDataStrategy.Synthetic;

    [BindProperty]
    public bool AllowProdToTestRefresh { get; set; }

    [BindProperty]
    public Guid? EditEnvironmentId { get; set; }

    [BindProperty]
    public string EnvironmentName { get; set; } = string.Empty;

    [BindProperty]
    public DeploymentEnvironmentType EnvironmentType { get; set; } = DeploymentEnvironmentType.Test;

    [BindProperty]
    public string? BaseUrlTemplate { get; set; }

    [BindProperty]
    public string? SecretReferencePrefix { get; set; }

    [BindProperty]
    public bool EnvironmentIsActive { get; set; } = true;

    [BindProperty]
    public int SortOrder { get; set; }

    [BindProperty]
    public bool RequiresApprovalForDeploy { get; set; }

    [BindProperty]
    public Guid DeleteEnvironmentId { get; set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Admin/Delivery");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync(CancellationToken cancellationToken)
    {
        try
        {
            Profile = await _mediator.Send(
                new UpdateTenantDeliveryProfileCommand(
                    DefaultCicdProvider,
                    VaultSecretPrefix,
                    AutoImplementOnApprove,
                    AutoDeployToTest,
                    RequirePullRequestReview,
                    RequireUatSignoff,
                    RequireProdChangeWindow,
                    ChangeWindowNotes,
                    QaVideoRetentionDays,
                    AllowOneClickProdDeploy,
                    AllowOneClickRollback,
                    TestDataStrategy,
                    AllowProdToTestRefresh),
                cancellationToken);
            StatusMessage = "Tenant delivery policies saved.";
        }
        catch (ValidationException ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveEnvironmentAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(
                new UpsertTenantDeploymentEnvironmentCommand(
                    EditEnvironmentId,
                    EnvironmentName,
                    EnvironmentType,
                    BaseUrlTemplate,
                    SecretReferencePrefix,
                    EnvironmentIsActive,
                    SortOrder,
                    RequiresApprovalForDeploy),
                cancellationToken);
            StatusMessage = "Environment saved.";
            EditEnvironmentId = null;
            EnvironmentName = string.Empty;
            BaseUrlTemplate = null;
            SecretReferencePrefix = null;
        }
        catch (ValidationException ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteEnvironmentAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTenantDeploymentEnvironmentCommand(DeleteEnvironmentId), cancellationToken);
        StatusMessage = "Environment deleted.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Profile = await _mediator.Send(new GetTenantDeliveryProfileQuery(), cancellationToken);
        Validation = await _mediator.Send(new ValidateTenantDeliveryProfileQuery(), cancellationToken);

        if (Profile is not null && string.IsNullOrWhiteSpace(EnvironmentName))
        {
            DefaultCicdProvider = Profile.DefaultCicdProvider;
            VaultSecretPrefix = Profile.VaultSecretPrefix;
            AutoImplementOnApprove = Profile.AutoImplementOnApprove;
            AutoDeployToTest = Profile.AutoDeployToTest;
            RequirePullRequestReview = Profile.RequirePullRequestReview;
            RequireUatSignoff = Profile.RequireUatSignoff;
            RequireProdChangeWindow = Profile.RequireProdChangeWindow;
            ChangeWindowNotes = Profile.ChangeWindowNotes;
            QaVideoRetentionDays = Profile.QaVideoRetentionDays;
            AllowOneClickProdDeploy = Profile.AllowOneClickProdDeploy;
            AllowOneClickRollback = Profile.AllowOneClickRollback;
            TestDataStrategy = Profile.TestDataStrategy;
            AllowProdToTestRefresh = Profile.AllowProdToTestRefresh;
        }
    }
}
