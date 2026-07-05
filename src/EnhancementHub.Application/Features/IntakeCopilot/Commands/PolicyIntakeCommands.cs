using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.IntakeCopilot.Commands;
using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.IntakeCopilot.Commands;

public sealed record AttachPolicyDocumentCommand(
    Guid SessionId,
    string FileName,
    Stream Content) : IRequest<IntakeCopilotTurnResponseDto>;

public sealed class AttachPolicyDocumentCommandHandler
    : IRequestHandler<AttachPolicyDocumentCommand, IntakeCopilotTurnResponseDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDocumentTextExtractor _extractor;
    private readonly IPiiRedactionService _piiRedaction;
    private readonly IMediator _mediator;

    public AttachPolicyDocumentCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IDocumentTextExtractor extractor,
        IPiiRedactionService piiRedaction,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _extractor = extractor;
        _piiRedaction = piiRedaction;
        _mediator = mediator;
    }

    public async Task<IntakeCopilotTurnResponseDto> Handle(
        AttachPolicyDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(request.SessionId, cancellationToken);
        var extraction = await _extractor.ExtractAsync(request.FileName, request.Content, cancellationToken);
        if (!extraction.Succeeded)
        {
            throw new ValidationException(extraction.ErrorMessage ?? "Failed to extract policy document text.");
        }

        session.PolicySourceLabel = request.FileName;
        session.PolicySourceText = _piiRedaction.Redact(extraction.Text);
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(
            new SendIntakeCopilotMessageCommand(
                session.Id,
                "Analyze the attached compliance/policy document and draft an enhancement request that addresses the key obligations."),
            cancellationToken);
    }

    private async Task<Domain.Entities.IntakeCopilotSession> LoadSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
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

public sealed record AttachPolicyUrlCommand(Guid SessionId, string Url) : IRequest<IntakeCopilotTurnResponseDto>;

public sealed class AttachPolicyUrlCommandHandler
    : IRequestHandler<AttachPolicyUrlCommand, IntakeCopilotTurnResponseDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IPolicyUrlFetcher _urlFetcher;
    private readonly IPiiRedactionService _piiRedaction;
    private readonly IMediator _mediator;

    public AttachPolicyUrlCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IPolicyUrlFetcher urlFetcher,
        IPiiRedactionService piiRedaction,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _urlFetcher = urlFetcher;
        _piiRedaction = piiRedaction;
        _mediator = mediator;
    }

    public async Task<IntakeCopilotTurnResponseDto> Handle(
        AttachPolicyUrlCommand request,
        CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(request.SessionId, cancellationToken);
        var fetch = await _urlFetcher.FetchAsync(request.Url, cancellationToken);
        if (!fetch.Succeeded)
        {
            throw new ValidationException(fetch.ErrorMessage ?? "Failed to fetch policy URL.");
        }

        session.PolicySourceLabel = fetch.SourceTitle ?? request.Url.Trim();
        session.PolicySourceText = _piiRedaction.Redact(fetch.Text);
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(
            new SendIntakeCopilotMessageCommand(
                session.Id,
                "Analyze the policy content from the provided URL and draft an enhancement request for compliance gaps."),
            cancellationToken);
    }

    private async Task<Domain.Entities.IntakeCopilotSession> LoadSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
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
