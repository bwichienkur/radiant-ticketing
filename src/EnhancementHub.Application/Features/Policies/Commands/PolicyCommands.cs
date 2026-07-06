using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Policies.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Policies.Commands;

public sealed record UpsertApprovalPolicyRuleCommand(
    Guid? Id,
    string Name,
    bool IsEnabled,
    int Priority,
    RiskLevel? MinimumRiskLevel,
    string? Department,
    ApplicationTier? ApplicationTier,
    UserRole RequiredRole,
    bool BlockApproval,
    string Message,
    int? SlaTargetHours = null,
    bool EscalateOnBreach = false,
    UserRole? EscalateToRole = null) : IRequest<ApprovalPolicyRuleDto>;

public sealed class UpsertApprovalPolicyRuleCommandValidator : AbstractValidator<UpsertApprovalPolicyRuleCommand>
{
    public UpsertApprovalPolicyRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Department).MaximumLength(200);
    }
}

public sealed class UpsertApprovalPolicyRuleCommandHandler
    : IRequestHandler<UpsertApprovalPolicyRuleCommand, ApprovalPolicyRuleDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public UpsertApprovalPolicyRuleCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<ApprovalPolicyRuleDto> Handle(
        UpsertApprovalPolicyRuleCommand request,
        CancellationToken cancellationToken)
    {
        ApprovalPolicyRule entity;
        var now = DateTime.UtcNow;

        if (request.Id.HasValue)
        {
            entity = await _dbContext.ApprovalPolicyRules
                .FirstOrDefaultAsync(r => r.Id == request.Id.Value, cancellationToken)
                ?? throw new Common.Exceptions.NotFoundException(nameof(ApprovalPolicyRule), request.Id.Value);
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new ApprovalPolicyRule
            {
                Id = Guid.NewGuid(),
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.ApprovalPolicyRules.Add(entity);
        }

        entity.Name = request.Name;
        entity.IsEnabled = request.IsEnabled;
        entity.Priority = request.Priority;
        entity.MinimumRiskLevel = request.MinimumRiskLevel;
        entity.Department = request.Department;
        entity.ApplicationTier = request.ApplicationTier;
        entity.RequiredRole = request.RequiredRole;
        entity.BlockApproval = request.BlockApproval;
        entity.Message = request.Message;
        entity.SlaTargetHours = request.SlaTargetHours;
        entity.EscalateOnBreach = request.EscalateOnBreach;
        entity.EscalateToRole = request.EscalateToRole;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    internal static ApprovalPolicyRuleDto ToDto(ApprovalPolicyRule entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.IsEnabled,
            entity.Priority,
            entity.MinimumRiskLevel,
            entity.Department,
            entity.ApplicationTier,
            entity.RequiredRole,
            entity.BlockApproval,
            entity.Message,
            entity.SlaTargetHours,
            entity.EscalateOnBreach,
            entity.EscalateToRole);
}

public sealed record DeleteApprovalPolicyRuleCommand(Guid Id) : IRequest;

public sealed class DeleteApprovalPolicyRuleCommandHandler
    : IRequestHandler<DeleteApprovalPolicyRuleCommand>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public DeleteApprovalPolicyRuleCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task Handle(DeleteApprovalPolicyRuleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ApprovalPolicyRules
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(ApprovalPolicyRule), request.Id);

        _dbContext.ApprovalPolicyRules.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
