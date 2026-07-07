using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

/// <summary>Legacy Razor register page — redirects to <c>/Spa/DatabaseConnections/Register</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/DatabaseConnections/Register. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class RegisterModel : PageModel
{
    private readonly IMediator _mediator;
    public RegisterModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> ApplicationOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/DatabaseConnectionRegister");
        }

        var apps = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        ApplicationOptions = apps.Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var connection = await _mediator.Send(new RegisterDatabaseConnectionCommand(
            Input.ApplicationId, Input.Name, Input.Provider, Input.ConnectionString, Input.IsReadOnly), cancellationToken);
        await _mediator.Send(new TriggerDatabaseScanCommand(connection.Id), cancellationToken);
        return RedirectToPage("Details", new { id = connection.Id });
    }

    public sealed class InputModel
    {
        public Guid ApplicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DatabaseProviderType Provider { get; set; } = DatabaseProviderType.Sqlite;
        public string ConnectionString { get; set; } = "Data Source=enhancementhub.db";
        public bool IsReadOnly { get; set; } = true;
    }
}
