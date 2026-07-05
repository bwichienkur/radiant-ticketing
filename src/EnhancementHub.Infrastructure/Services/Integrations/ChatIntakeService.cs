using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class ChatIntakeService : IChatIntakeService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IIntakeCopilotService _intakeCopilot;
    private readonly IntegrationsOptions _options;

    public ChatIntakeService(
        IEnhancementHubDbContext dbContext,
        IIntakeCopilotService intakeCopilot,
        IOptions<IntegrationsOptions> options)
    {
        _dbContext = dbContext;
        _intakeCopilot = intakeCopilot;
        _options = options.Value;
    }

    public Task<ChatIntakeResult> SubmitFromSlackAsync(
        SlackIntakePayload payload,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Slack.Enabled)
        {
            return Task.FromResult(new ChatIntakeResult(false, null, "Slack intake is disabled."));
        }

        return CreateRequestAsync(
            payload.Text,
            payload.UserName ?? "slack-user",
            _options.Slack.DefaultPriority,
            null,
            null,
            IntakeCopilotSource.Slack,
            cancellationToken);
    }

    public Task<ChatIntakeResult> SubmitFromTeamsAsync(
        TeamsIntakePayload payload,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Teams.Enabled)
        {
            return Task.FromResult(new ChatIntakeResult(false, null, "Teams intake is disabled."));
        }

        return CreateRequestAsync(
            payload.Text,
            payload.UserName ?? "teams-user",
            _options.Teams.DefaultPriority,
            payload.TargetApplicationId,
            payload.TeamId,
            IntakeCopilotSource.Teams,
            cancellationToken);
    }

    private async Task<ChatIntakeResult> CreateRequestAsync(
        string text,
        string submitterName,
        string defaultPriority,
        Guid? targetApplicationId,
        Guid? teamId,
        IntakeCopilotSource source,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ChatIntakeResult(false, null, "Message text is required.");
        }

        var conversation = new List<IntakeCopilotMessage>
        {
            new()
            {
                Role = "user",
                Content = text.Trim(),
                OccurredAt = DateTime.UtcNow
            }
        };

        var turn = await _intakeCopilot.ProcessTurnAsync(
            conversation,
            null,
            turnCount: 1,
            source,
            cancellationToken);

        var draft = turn.Draft ?? new IntakeCopilotDraft
        {
            Title = TruncateTitle(text),
            BusinessDescription = text.Trim(),
            DesiredOutcome = "Submitted via chat integration; review and refine scope.",
            Priority = defaultPriority
        };

        if (targetApplicationId.HasValue)
        {
            draft.TargetApplicationId = targetApplicationId;
        }

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            draft.Title = TruncateTitle(text);
        }

        if (string.IsNullOrWhiteSpace(draft.DesiredOutcome))
        {
            draft.DesiredOutcome = "Submitted via chat integration; review and refine scope.";
        }

        draft.Priority = string.IsNullOrWhiteSpace(draft.Priority) ? defaultPriority : draft.Priority;
        draft.SupportingNotes = string.IsNullOrWhiteSpace(draft.SupportingNotes)
            ? $"Submitted by {submitterName} via {source} intake copilot."
            : $"{draft.SupportingNotes}\nSubmitted by {submitterName} via {source}.";

        var submitter = await ResolveOrCreateIntakeUserAsync(submitterName, cancellationToken);
        var now = DateTime.UtcNow;

        var request = new EnhancementRequest
        {
            Id = Guid.NewGuid(),
            Title = draft.Title,
            BusinessDescription = draft.BusinessDescription,
            DesiredOutcome = draft.DesiredOutcome,
            Priority = draft.Priority,
            TargetApplicationId = draft.TargetApplicationId,
            SubmittedByUserId = submitter.Id,
            TeamId = teamId,
            Department = draft.Department,
            SupportingNotes = draft.SupportingNotes,
            Status = EnhancementRequestStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = submitter.Id
        };

        _dbContext.EnhancementRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ChatIntakeResult(
            true,
            request.Id,
            $"Enhancement request '{request.Title}' created via intake copilot.");
    }

    public static (string Title, string Description) ParseIntakeText(string text)
    {
        var parts = text.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return (TruncateTitle(parts[0]), parts[1]);
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 1)
        {
            return (TruncateTitle(lines[0]), string.Join(' ', lines.Skip(1)));
        }

        return (TruncateTitle(text), text);
    }

    private static string TruncateTitle(string value) =>
        value.Length <= 500 ? value : value[..500];

    private async Task<User> ResolveOrCreateIntakeUserAsync(
        string submitterName,
        CancellationToken cancellationToken)
    {
        var email = $"{SanitizeEmailLocalPart(submitterName)}@integrations.enhancementhub.local";
        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = submitterName,
            Role = UserRole.Submitter,
            IsActive = true,
            PasswordHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string SanitizeEmailLocalPart(string value)
    {
        var chars = value.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_')
            .ToArray();
        return chars.Length == 0 ? "integration-user" : new string(chars);
    }
}
