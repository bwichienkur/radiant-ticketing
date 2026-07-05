using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnhancementHub.Application.Features.Analysis.Commands;

public sealed record ApproveAnalysisFindingCommand(Guid FindingId, bool Approved = true)
    : IRequest<AnalysisFindingDto>;

public sealed class ApproveAnalysisFindingCommandHandler
    : IRequestHandler<ApproveAnalysisFindingCommand, AnalysisFindingDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ApproveAnalysisFindingCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<AnalysisFindingDto> Handle(
        ApproveAnalysisFindingCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var finding = await _dbContext.AnalysisFindings
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AnalysisFinding), request.FindingId);

        finding.IsHumanApproved = request.Approved;
        finding.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return finding.ToDto();
    }
}

public sealed record RecordArchitectAnalysisEditCommand(
    Guid EnhancementRequestId,
    Guid EnhancementAnalysisId,
    string? FeatureSummary,
    string? TechnicalRequirements,
    string? TestingPlan,
    string? RolloutPlan,
    string? Comments) : IRequest<AnalysisComparisonDto>;

public sealed class RecordArchitectAnalysisEditCommandValidator
    : AbstractValidator<RecordArchitectAnalysisEditCommand>
{
    public RecordArchitectAnalysisEditCommandValidator()
    {
        RuleFor(x => x.EnhancementRequestId).NotEmpty();
        RuleFor(x => x.EnhancementAnalysisId).NotEmpty();
    }
}

public sealed class RecordArchitectAnalysisEditCommandHandler
    : IRequestHandler<RecordArchitectAnalysisEditCommand, AnalysisComparisonDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public RecordArchitectAnalysisEditCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AnalysisComparisonDto> Handle(
        RecordArchitectAnalysisEditCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (_currentUser.Role is not (UserRole.Approver or UserRole.Admin or UserRole.Developer))
        {
            throw new ForbiddenException("Only approvers, developers, or admins can record architect edits.");
        }

        var analysis = await _dbContext.EnhancementAnalyses
            .FirstOrDefaultAsync(
                a => a.Id == request.EnhancementAnalysisId
                     && a.EnhancementRequestId == request.EnhancementRequestId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.EnhancementAnalysis), request.EnhancementAnalysisId);

        var previous = new Dictionary<string, string?>
        {
            ["FeatureSummary"] = analysis.FeatureSummary,
            ["TechnicalRequirements"] = analysis.TechnicalRequirements,
            ["TestingPlan"] = analysis.TestingPlan,
            ["RolloutPlan"] = analysis.RolloutPlan
        };

        if (!string.IsNullOrWhiteSpace(request.FeatureSummary))
        {
            analysis.FeatureSummary = request.FeatureSummary;
        }

        if (!string.IsNullOrWhiteSpace(request.TechnicalRequirements))
        {
            analysis.TechnicalRequirements = request.TechnicalRequirements;
        }

        if (!string.IsNullOrWhiteSpace(request.TestingPlan))
        {
            analysis.TestingPlan = request.TestingPlan;
        }

        if (!string.IsNullOrWhiteSpace(request.RolloutPlan))
        {
            analysis.RolloutPlan = request.RolloutPlan;
        }

        analysis.IsApprovedSnapshot = true;
        analysis.UpdatedAt = DateTime.UtcNow;

        var updated = new Dictionary<string, string?>
        {
            ["FeatureSummary"] = analysis.FeatureSummary,
            ["TechnicalRequirements"] = analysis.TechnicalRequirements,
            ["TestingPlan"] = analysis.TestingPlan,
            ["RolloutPlan"] = analysis.RolloutPlan
        };

        var now = DateTime.UtcNow;
        _dbContext.ApprovalActions.Add(new Domain.Entities.ApprovalAction
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.EnhancementRequestId,
            EnhancementAnalysisId = analysis.Id,
            UserId = _currentUser.UserId.Value,
            ActionType = ApprovalActionType.EditRequirements,
            Comments = request.Comments,
            PreviousValue = JsonSerializer.Serialize(previous),
            NewValue = JsonSerializer.Serialize(updated),
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(
            new Queries.GetAnalysisComparisonQuery(request.EnhancementRequestId, analysis.Version),
            cancellationToken);
    }
}
