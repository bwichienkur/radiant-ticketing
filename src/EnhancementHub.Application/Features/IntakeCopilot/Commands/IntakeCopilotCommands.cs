using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using EnhancementHub.Application.Features.Templates.Queries;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.IntakeCopilot.Commands;

public sealed record StartIntakeCopilotSessionCommand(
    string? InitialPrompt = null,
    IntakeCopilotSource Source = IntakeCopilotSource.Web) : IRequest<IntakeCopilotTurnResponseDto>;

public sealed class StartIntakeCopilotSessionCommandHandler
    : IRequestHandler<StartIntakeCopilotSessionCommand, IntakeCopilotTurnResponseDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IIntakeCopilotService _intakeCopilot;
    private readonly IMediator _mediator;

    public StartIntakeCopilotSessionCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IIntakeCopilotService intakeCopilot,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _intakeCopilot = intakeCopilot;
        _mediator = mediator;
    }

    public async Task<IntakeCopilotTurnResponseDto> Handle(
        StartIntakeCopilotSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new ForbiddenException("Authentication required.");
        }

        var now = DateTime.UtcNow;
        var session = new IntakeCopilotSession
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            Status = IntakeCopilotSessionStatus.Active,
            Source = request.Source,
            TurnCount = 0,
            MessagesJson = "[]",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = _currentUser.UserId
        };

        _dbContext.IntakeCopilotSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.InitialPrompt))
        {
            var welcome = new IntakeCopilotTurnResult
            {
                AssistantMessage =
                    "Describe the enhancement you need in plain language. I'll ask a few questions based on your applications and draft a request for you to review.",
                FollowUpQuestions = [],
                IsComplete = false
            };

            var welcomeMessage = new IntakeCopilotMessage
            {
                Role = "assistant",
                Content = welcome.AssistantMessage,
                OccurredAt = DateTime.UtcNow
            };
            session.MessagesJson = IntakeCopilotMapper.SerializeMessages([welcomeMessage]);
            session.LastAssistantMessage = welcome.AssistantMessage;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return IntakeCopilotMapper.ToTurnResponse(session, welcome);
        }

        return await _mediator.Send(
            new SendIntakeCopilotMessageCommand(session.Id, request.InitialPrompt.Trim()),
            cancellationToken);
    }
}

public sealed record SendIntakeCopilotMessageCommand(
    Guid SessionId,
    string Message) : IRequest<IntakeCopilotTurnResponseDto>;

public sealed class SendIntakeCopilotMessageCommandHandler
    : IRequestHandler<SendIntakeCopilotMessageCommand, IntakeCopilotTurnResponseDto>
{
    private const int MaxTurns = 5;

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IIntakeCopilotService _intakeCopilot;

    public SendIntakeCopilotMessageCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IIntakeCopilotService intakeCopilot)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _intakeCopilot = intakeCopilot;
    }

    public async Task<IntakeCopilotTurnResponseDto> Handle(
        SendIntakeCopilotMessageCommand request,
        CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(request.SessionId, cancellationToken);

        if (session.Status is IntakeCopilotSessionStatus.Completed or IntakeCopilotSessionStatus.Abandoned)
        {
            throw new ValidationException("Session is no longer active.");
        }

        var messages = IntakeCopilotMapper.DeserializeMessages(session.MessagesJson);
        var draft = IntakeCopilotMapper.DeserializeDraft(session.DraftJson);
        var userMessage = new IntakeCopilotMessage
        {
            Role = "user",
            Content = request.Message.Trim(),
            OccurredAt = DateTime.UtcNow
        };

        messages.Add(userMessage);

        var turnResult = await _intakeCopilot.ProcessTurnAsync(
            messages,
            draft,
            session.TurnCount + 1,
            session.Source,
            cancellationToken);

        messages.Add(new IntakeCopilotMessage
        {
            Role = "assistant",
            Content = turnResult.AssistantMessage,
            OccurredAt = DateTime.UtcNow
        });

        session.TurnCount += 1;
        session.MessagesJson = IntakeCopilotMapper.SerializeMessages(messages);
        session.LastAssistantMessage = turnResult.AssistantMessage;
        session.DraftJson = turnResult.Draft is null
            ? session.DraftJson
            : IntakeCopilotMapper.SerializeDraft(turnResult.Draft);
        session.SuggestedTemplateId = turnResult.SuggestedTemplateId ?? session.SuggestedTemplateId;

        if (turnResult.IsComplete || session.TurnCount >= MaxTurns)
        {
            session.Status = IntakeCopilotSessionStatus.ReadyForSubmit;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return IntakeCopilotMapper.ToTurnResponse(session, turnResult);
    }

    private async Task<IntakeCopilotSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new ForbiddenException("Authentication required.");
        }

        var session = await _dbContext.IntakeCopilotSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new NotFoundException("IntakeCopilotSession", sessionId);

        if (session.UserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this intake session.");
        }

        return session;
    }
}

public sealed record CreateRequestFromIntakeSessionCommand(Guid SessionId) : IRequest<Guid>;

public sealed class CreateRequestFromIntakeSessionCommandHandler
    : IRequestHandler<CreateRequestFromIntakeSessionCommand, Guid>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CreateRequestFromIntakeSessionCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(
        CreateRequestFromIntakeSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new ForbiddenException("Authentication required.");
        }

        var session = await _dbContext.IntakeCopilotSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("IntakeCopilotSession", request.SessionId);

        if (session.UserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this intake session.");
        }

        if (session.CreatedRequestId.HasValue)
        {
            return session.CreatedRequestId.Value;
        }

        var draft = IntakeCopilotMapper.DeserializeDraft(session.DraftJson)
            ?? throw new ValidationException("No draft is available for this session. Continue the conversation first.");

        if (string.IsNullOrWhiteSpace(draft.Title)
            || string.IsNullOrWhiteSpace(draft.BusinessDescription)
            || string.IsNullOrWhiteSpace(draft.DesiredOutcome))
        {
            throw new ValidationException("Draft is incomplete. Provide more detail in the intake conversation.");
        }

        var created = await _mediator.Send(
            new CreateEnhancementRequestCommand(
                draft.Title.Trim(),
                draft.BusinessDescription.Trim(),
                draft.DesiredOutcome.Trim(),
                draft.Priority,
                draft.TargetApplicationId,
                null,
                draft.Department,
                null,
                draft.SupportingNotes,
                session.SuggestedTemplateId),
            cancellationToken);

        session.Status = IntakeCopilotSessionStatus.Completed;
        session.CreatedRequestId = created.Id;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return created.Id;
    }
}

internal static class IntakeCopilotMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static List<IntakeCopilotMessage> DeserializeMessages(string json) =>
        JsonSerializer.Deserialize<List<IntakeCopilotMessage>>(json, JsonOptions) ?? [];

    public static string SerializeMessages(IReadOnlyList<IntakeCopilotMessage> messages) =>
        JsonSerializer.Serialize(messages, JsonOptions);

    public static IntakeCopilotDraft? DeserializeDraft(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<IntakeCopilotDraft>(json, JsonOptions);

    public static string SerializeDraft(IntakeCopilotDraft draft) =>
        JsonSerializer.Serialize(draft, JsonOptions);

    public static IntakeCopilotTurnResponseDto ToTurnResponse(
        IntakeCopilotSession session,
        IntakeCopilotTurnResult turn) =>
        new(
            ToSessionDto(session),
            turn.AssistantMessage,
            turn.FollowUpQuestions,
            turn.IsComplete,
            turn.UsedMockAi);

    public static IntakeCopilotSessionDto ToSessionDto(IntakeCopilotSession session)
    {
        var draft = DeserializeDraft(session.DraftJson);
        return new IntakeCopilotSessionDto(
            session.Id,
            session.Status.ToString(),
            session.TurnCount,
            DeserializeMessages(session.MessagesJson)
                .Select(m => new IntakeCopilotMessageDto(m.Role, m.Content, m.OccurredAt))
                .ToList(),
            draft is null ? null : ToDraftDto(draft),
            session.SuggestedTemplateId,
            session.CreatedRequestId,
            session.LastAssistantMessage);
    }

    public static IntakeCopilotDraftDto ToDraftDto(IntakeCopilotDraft draft) =>
        new(
            draft.Title,
            draft.BusinessDescription,
            draft.DesiredOutcome,
            draft.Priority,
            draft.TargetApplicationId,
            draft.Department,
            draft.SupportingNotes,
            draft.SuggestedTemplateDomainCategory);
}
