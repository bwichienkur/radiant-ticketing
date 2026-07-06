using EnhancementHub.Application.Common;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class WebhooksModel : PageModel
{
    private readonly IMediator _mediator;

    public WebhooksModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<WebhookSubscriptionSummaryDto> Subscriptions { get; private set; } = [];
    public IReadOnlyList<WebhookDeliverySummaryDto> Deliveries { get; private set; } = [];

    [BindProperty]
    public string NewSubscriptionName { get; set; } = string.Empty;

    [BindProperty]
    public string NewSubscriptionUrl { get; set; } = string.Empty;

    [BindProperty]
    public List<string> SelectedEventTypes { get; set; } = [WebhookEventTypes.RequestApproved];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? CreatedWebhookSecret { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Subscriptions = await _mediator.Send(new ListWebhookSubscriptionsQuery(), cancellationToken);
        Deliveries = await _mediator.Send(new ListWebhookDeliveriesQuery(50), cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateWebhookSubscriptionCommand(
                NewSubscriptionName,
                NewSubscriptionUrl,
                SelectedEventTypes), cancellationToken);

            CreatedWebhookSecret = result.Secret;
            StatusMessage = $"Created webhook '{result.Name}' (secret prefix {result.SecretPrefix}…).";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid revokeSubscriptionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeWebhookSubscriptionCommand(revokeSubscriptionId), cancellationToken);
        StatusMessage = "Webhook subscription revoked.";
        return RedirectToPage();
    }
}
