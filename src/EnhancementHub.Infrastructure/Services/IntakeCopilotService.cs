using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.Templates.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class IntakeCopilotService : IIntakeCopilotService
{
    private const int MaxFollowUpQuestions = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatCompletionService _chatCompletion;
    private readonly IKnowledgeSearchService _knowledgeSearch;
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<IntakeCopilotService> _logger;

    public IntakeCopilotService(
        IChatCompletionService chatCompletion,
        IKnowledgeSearchService knowledgeSearch,
        IEnhancementHubDbContext dbContext,
        IMediator mediator,
        ILogger<IntakeCopilotService> logger)
    {
        _chatCompletion = chatCompletion;
        _knowledgeSearch = knowledgeSearch;
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IntakeCopilotTurnResult> ProcessTurnAsync(
        IReadOnlyList<IntakeCopilotMessage> conversation,
        IntakeCopilotDraft? currentDraft,
        int turnCount,
        IntakeCopilotSource source,
        string? policySourceText = null,
        string? policySourceLabel = null,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = conversation.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var context = await BuildContextAsync(lastUserMessage, cancellationToken);

        if (!_chatCompletion.IsConfigured)
        {
            return await EnrichTurnWithTemplateAsync(
                BuildMockTurn(
                    lastUserMessage,
                    currentDraft,
                    context,
                    turnCount,
                    policySourceText,
                    policySourceLabel),
                cancellationToken);
        }

        try
        {
            var systemPrompt = """
                You are EnhancementHub Intake Copilot. Help users describe software enhancement needs.
                Use the provided application and knowledge context to ask specific follow-up questions.
                When a compliance or policy document is attached, extract key obligations and map them to
                concrete software changes — prefer suggestedTemplateDomainCategory Compliance when appropriate.
                Return ONLY valid JSON with:
                assistantMessage (string),
                followUpQuestions (string array, max 3, empty when enough info),
                isComplete (boolean — true when title, businessDescription, desiredOutcome are sufficient),
                draft (object with title, businessDescription, desiredOutcome, priority Low|Medium|High|Critical,
                  targetApplicationId as GUID string or null, department, supportingNotes,
                  suggestedTemplateDomainCategory as Security|Performance|Compliance or null).
                Do not invent application IDs — only use IDs from context. Be concise.
                This is intake assistance, not legal or compliance advice.
                """;

            var userPrompt = BuildUserPrompt(
                conversation,
                currentDraft,
                context,
                turnCount,
                source,
                policySourceText,
                policySourceLabel);

            var completion = await _chatCompletion.CompleteAsync(
                new ChatCompletionRequest
                {
                    WorkflowStep = AiWorkflowStep.IntakeCopilot,
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(completion.Content))
            {
                return await EnrichTurnWithTemplateAsync(
                    BuildMockTurn(
                        lastUserMessage,
                        currentDraft,
                        context,
                        turnCount,
                        policySourceText,
                        policySourceLabel),
                    cancellationToken);
            }

            var parsed = JsonSerializer.Deserialize<IntakeCopilotAiResponse>(completion.Content, JsonOptions);
            if (parsed is null)
            {
                return await EnrichTurnWithTemplateAsync(
                    BuildMockTurn(
                        lastUserMessage,
                        currentDraft,
                        context,
                        turnCount,
                        policySourceText,
                        policySourceLabel),
                    cancellationToken);
            }

            var draft = MergeDraft(currentDraft, parsed.Draft);
            var templateId = await ResolveTemplateIdAsync(parsed.Draft?.SuggestedTemplateDomainCategory, cancellationToken);

            return new IntakeCopilotTurnResult
            {
                AssistantMessage = parsed.AssistantMessage
                    ?? "I've updated the draft based on your input. Review the form below.",
                FollowUpQuestions = (parsed.FollowUpQuestions ?? [])
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Take(MaxFollowUpQuestions)
                    .ToList(),
                IsComplete = parsed.IsComplete || turnCount >= 5,
                Draft = draft,
                SuggestedTemplateId = templateId,
                UsedMockAi = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Intake copilot AI call failed; using mock intake.");
            return await EnrichTurnWithTemplateAsync(
                BuildMockTurn(
                    lastUserMessage,
                    currentDraft,
                    context,
                    turnCount,
                    policySourceText,
                    policySourceLabel),
                cancellationToken);
        }
    }

    private async Task<IntakeCopilotTurnResult> EnrichTurnWithTemplateAsync(
        IntakeCopilotTurnResult turn,
        CancellationToken cancellationToken)
    {
        if (turn.SuggestedTemplateId.HasValue)
        {
            return turn;
        }

        var category = turn.Draft?.SuggestedTemplateDomainCategory;
        if (string.IsNullOrWhiteSpace(category))
        {
            return turn;
        }

        turn.SuggestedTemplateId = await ResolveTemplateIdAsync(category, cancellationToken);
        return turn;
    }

    private async Task<IntakeContextBundle> BuildContextAsync(string query, CancellationToken cancellationToken)
    {
        var applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        var appIds = applications.Select(a => a.Id).ToList();

        var profiles = await _dbContext.ApplicationProfiles
            .AsNoTracking()
            .Where(p => appIds.Contains(p.ApplicationId))
            .OrderByDescending(p => p.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var profileByApp = profiles
            .GroupBy(p => p.ApplicationId)
            .ToDictionary(g => g.Key, g => g.First());

        IReadOnlyList<KnowledgeSearchResult> knowledge = [];
        if (!string.IsNullOrWhiteSpace(query))
        {
            try
            {
                knowledge = await _knowledgeSearch.SearchAsync(query, topK: 5, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Knowledge search unavailable for intake context.");
            }
        }

        var graphCounts = await _dbContext.SystemGraphNodes
            .AsNoTracking()
            .Where(n => n.ApplicationId.HasValue && appIds.Contains(n.ApplicationId.Value))
            .GroupBy(n => n.ApplicationId!.Value)
            .Select(g => new { ApplicationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Count, cancellationToken);

        return new IntakeContextBundle(applications, profileByApp, knowledge, graphCounts);
    }

    private static string BuildUserPrompt(
        IReadOnlyList<IntakeCopilotMessage> conversation,
        IntakeCopilotDraft? currentDraft,
        IntakeContextBundle context,
        int turnCount,
        IntakeCopilotSource source,
        string? policySourceText,
        string? policySourceLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Turn: {turnCount}/5");
        sb.AppendLine($"Source: {source}");
        sb.AppendLine();
        sb.AppendLine("## Applications");
        foreach (var app in context.Applications)
        {
            context.Profiles.TryGetValue(app.Id, out var profile);
            context.GraphCounts.TryGetValue(app.Id, out var nodeCount);
            sb.AppendLine($"- Id={app.Id} Name={app.Name} Domain={app.BusinessDomain ?? "n/a"}");
            if (profile is not null)
            {
                var components = profile.KeyComponents;
                if (!string.IsNullOrWhiteSpace(components))
                {
                    sb.AppendLine($"  KeyComponents: {Truncate(components, 400)}");
                }
            }

            if (nodeCount > 0)
            {
                sb.AppendLine($"  SystemGraphNodes: {nodeCount}");
            }
        }

        if (context.Knowledge.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Knowledge snippets");
            foreach (var item in context.Knowledge)
            {
                sb.AppendLine($"- {item.Title}: {Truncate(item.Snippet, 200)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(policySourceText))
        {
            sb.AppendLine();
            sb.AppendLine("## Attached policy / compliance document");
            if (!string.IsNullOrWhiteSpace(policySourceLabel))
            {
                sb.AppendLine($"Source: {policySourceLabel}");
            }

            sb.AppendLine(Truncate(policySourceText, 12_000));
        }

        if (currentDraft is not null)
        {
            sb.AppendLine();
            sb.AppendLine("## Current draft");
            sb.AppendLine(JsonSerializer.Serialize(currentDraft, JsonOptions));
        }

        sb.AppendLine();
        sb.AppendLine("## Conversation");
        foreach (var message in conversation.TakeLast(12))
        {
            sb.AppendLine($"{message.Role}: {message.Content}");
        }

        return sb.ToString();
    }

    private IntakeCopilotTurnResult BuildMockTurn(
        string userMessage,
        IntakeCopilotDraft? currentDraft,
        IntakeContextBundle context,
        int turnCount,
        string? policySourceText,
        string? policySourceLabel)
    {
        var hasPolicy = !string.IsNullOrWhiteSpace(policySourceText);
        var draft = currentDraft ?? new IntakeCopilotDraft();
        if (hasPolicy && string.IsNullOrWhiteSpace(draft.Title))
        {
            var label = string.IsNullOrWhiteSpace(policySourceLabel) ? "policy document" : policySourceLabel;
            draft.Title = TruncateTitle($"Compliance enhancements from {label}");
            draft.BusinessDescription =
                "Address obligations identified in the attached policy document through targeted system changes.";
            draft.DesiredOutcome =
                "Close compliance gaps with auditable controls and measurable evidence of adherence.";
            draft.SuggestedTemplateDomainCategory = "Compliance";
            draft.Priority = "High";
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                draft.SupportingNotes = userMessage.Trim();
            }
        }
        else if (string.IsNullOrWhiteSpace(draft.Title))
        {
            var lines = userMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            draft.Title = TruncateTitle(lines.Length > 0 ? lines[0] : userMessage);
            draft.BusinessDescription = lines.Length > 1
                ? string.Join(' ', lines.Skip(1))
                : userMessage;
        }
        else if (!string.IsNullOrWhiteSpace(userMessage))
        {
            draft.BusinessDescription = $"{draft.BusinessDescription}\n{userMessage}".Trim();
        }

        if (string.IsNullOrWhiteSpace(draft.DesiredOutcome))
        {
            draft.DesiredOutcome = hasPolicy
                ? "Implement controls that satisfy the policy requirements with traceable audit evidence."
                : "Deliver the described capability with measurable business value.";
        }

        if (hasPolicy && string.IsNullOrWhiteSpace(draft.SuggestedTemplateDomainCategory))
        {
            draft.SuggestedTemplateDomainCategory = "Compliance";
        }

        if (!draft.TargetApplicationId.HasValue && context.Applications.Count == 1)
        {
            draft.TargetApplicationId = context.Applications[0].Id;
        }

        var questions = new List<string>();
        if (!draft.TargetApplicationId.HasValue && context.Applications.Count > 1)
        {
            var names = string.Join(", ", context.Applications.Take(4).Select(a => a.Name));
            questions.Add($"Which application should this target? Options include: {names}.");
        }

        if (draft.BusinessDescription.Length < 40 && !hasPolicy)
        {
            questions.Add("What business problem or regulation is driving this change?");
        }

        if (hasPolicy && turnCount < 2)
        {
            questions.Add("Which policy sections are highest priority for the first implementation wave?");
        }

        var isComplete = hasPolicy
            ? turnCount >= 1
              && !string.IsNullOrWhiteSpace(draft.Title)
              && draft.BusinessDescription.Length >= 20
              && (draft.TargetApplicationId.HasValue || context.Applications.Count <= 1)
            : turnCount >= 2
              && !string.IsNullOrWhiteSpace(draft.Title)
              && draft.BusinessDescription.Length >= 20
              && (draft.TargetApplicationId.HasValue || context.Applications.Count <= 1);

        if (isComplete)
        {
            questions.Clear();
        }

        return new IntakeCopilotTurnResult
        {
            AssistantMessage = isComplete
                ? hasPolicy
                    ? "I've drafted a compliance-oriented request from the policy document. Review the form below and submit when ready."
                    : "I've drafted a request from your description. Review the form below and submit when ready."
                : hasPolicy
                    ? "I've started a compliance draft from the policy document. A few more details will help finalize it."
                    : "Thanks — I need a bit more detail to draft a complete request.",
            FollowUpQuestions = questions.Take(MaxFollowUpQuestions).ToList(),
            IsComplete = isComplete,
            Draft = draft,
            UsedMockAi = true
        };
    }

    private static IntakeCopilotDraft MergeDraft(IntakeCopilotDraft? existing, IntakeCopilotAiDraft? incoming)
    {
        var draft = existing ?? new IntakeCopilotDraft();
        if (incoming is null)
        {
            return draft;
        }

        if (!string.IsNullOrWhiteSpace(incoming.Title))
        {
            draft.Title = incoming.Title;
        }

        if (!string.IsNullOrWhiteSpace(incoming.BusinessDescription))
        {
            draft.BusinessDescription = incoming.BusinessDescription;
        }

        if (!string.IsNullOrWhiteSpace(incoming.DesiredOutcome))
        {
            draft.DesiredOutcome = incoming.DesiredOutcome;
        }

        if (!string.IsNullOrWhiteSpace(incoming.Priority))
        {
            draft.Priority = incoming.Priority;
        }

        if (Guid.TryParse(incoming.TargetApplicationId, out var appId))
        {
            draft.TargetApplicationId = appId;
        }

        if (!string.IsNullOrWhiteSpace(incoming.Department))
        {
            draft.Department = incoming.Department;
        }

        if (!string.IsNullOrWhiteSpace(incoming.SupportingNotes))
        {
            draft.SupportingNotes = incoming.SupportingNotes;
        }

        if (!string.IsNullOrWhiteSpace(incoming.SuggestedTemplateDomainCategory))
        {
            draft.SuggestedTemplateDomainCategory = incoming.SuggestedTemplateDomainCategory;
        }

        return draft;
    }

    private async Task<Guid?> ResolveTemplateIdAsync(string? domainCategory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(domainCategory))
        {
            return null;
        }

        var templates = await _mediator.Send(
            new ListEnhancementTemplatesQuery(domainCategory),
            cancellationToken);

        return templates.FirstOrDefault()?.Id;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string TruncateTitle(string value) =>
        value.Length <= 500 ? value : value[..500];

    private sealed class IntakeContextBundle
    {
        public IntakeContextBundle(
            IReadOnlyList<ApplicationDto> applications,
            IReadOnlyDictionary<Guid, Domain.Entities.ApplicationProfile> profiles,
            IReadOnlyList<KnowledgeSearchResult> knowledge,
            IReadOnlyDictionary<Guid, int> graphCounts)
        {
            Applications = applications;
            Profiles = profiles;
            Knowledge = knowledge;
            GraphCounts = graphCounts;
        }

        public IReadOnlyList<ApplicationDto> Applications { get; }
        public IReadOnlyDictionary<Guid, Domain.Entities.ApplicationProfile> Profiles { get; }
        public IReadOnlyList<KnowledgeSearchResult> Knowledge { get; }
        public IReadOnlyDictionary<Guid, int> GraphCounts { get; }
    }

    private sealed class IntakeCopilotAiResponse
    {
        public string? AssistantMessage { get; set; }
        public List<string>? FollowUpQuestions { get; set; }
        public bool IsComplete { get; set; }
        public IntakeCopilotAiDraft? Draft { get; set; }
    }

    private sealed class IntakeCopilotAiDraft
    {
        public string? Title { get; set; }
        public string? BusinessDescription { get; set; }
        public string? DesiredOutcome { get; set; }
        public string? Priority { get; set; }
        public string? TargetApplicationId { get; set; }
        public string? Department { get; set; }
        public string? SupportingNotes { get; set; }
        public string? SuggestedTemplateDomainCategory { get; set; }
    }
}
