using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class ChatIntakeService : IChatIntakeService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IntegrationsOptions _options;

    public ChatIntakeService(
        IEnhancementHubDbContext dbContext,
        IOptions<IntegrationsOptions> options)
    {
        _dbContext = dbContext;
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
            cancellationToken);
    }

    private async Task<ChatIntakeResult> CreateRequestAsync(
        string text,
        string submitterName,
        string defaultPriority,
        Guid? targetApplicationId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ChatIntakeResult(false, null, "Message text is required.");
        }

        var (title, description) = ParseIntakeText(text);
        var submitter = await ResolveOrCreateIntakeUserAsync(submitterName, cancellationToken);
        var now = DateTime.UtcNow;

        var request = new EnhancementRequest
        {
            Id = Guid.NewGuid(),
            Title = title,
            BusinessDescription = description,
            DesiredOutcome = "Submitted via chat integration; review and refine scope.",
            Priority = defaultPriority,
            TargetApplicationId = targetApplicationId,
            SubmittedByUserId = submitter.Id,
            TeamId = teamId,
            SupportingNotes = $"Submitted by {submitterName} via integration.",
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
            $"Enhancement request '{title}' created.");
    }

    internal static (string Title, string Description) ParseIntakeText(string text)
    {
        var parts = text.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return (TrimTitle(parts[0]), parts[1]);
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 1)
        {
            return (TrimTitle(lines[0]), string.Join(' ', lines.Skip(1)));
        }

        return (TrimTitle(text), text);
    }

    private static string TrimTitle(string value) =>
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
