using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AiPromptsModel : PageModel
{
    private readonly IMediator _mediator;

    public AiPromptsModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<AiPromptConfigurationDto> Prompts { get; private set; } = [];

    [BindProperty]
    public Guid PromptId { get; set; }

    [BindProperty]
    public string SystemPromptTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string UserPromptTemplate { get; set; } = string.Empty;

    [BindProperty]
    public bool IsActive { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Prompts = await _mediator.Send(new ListAiPromptConfigurationsQuery(), cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateAiPromptConfigurationCommand(
            PromptId, SystemPromptTemplate, UserPromptTemplate, IsActive), cancellationToken);
        return RedirectToPage();
    }
}
