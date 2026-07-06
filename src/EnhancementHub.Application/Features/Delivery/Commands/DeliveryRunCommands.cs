using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using EnhancementHub.Application.Features.Delivery.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Commands;

public sealed record StartDeliveryRunCommand(Guid EnhancementRequestId) : IRequest<EnhancementDeliveryRunDto>;

public sealed class StartDeliveryRunCommandHandler : IRequestHandler<StartDeliveryRunCommand, EnhancementDeliveryRunDto>
{
    private readonly IDeliveryOrchestrationService _orchestration;
    private readonly IDeliveryOrchestrationDispatcher _dispatcher;
    private readonly IMediator _mediator;

    public StartDeliveryRunCommandHandler(
        IDeliveryOrchestrationService orchestration,
        IDeliveryOrchestrationDispatcher dispatcher,
        IMediator mediator)
    {
        _orchestration = orchestration;
        _dispatcher = dispatcher;
        _mediator = mediator;
    }

    public async Task<EnhancementDeliveryRunDto> Handle(
        StartDeliveryRunCommand request,
        CancellationToken cancellationToken)
    {
        await _orchestration.StartDeliveryRunAsync(request.EnhancementRequestId, cancellationToken);
        _dispatcher.EnqueueProcessing(request.EnhancementRequestId);

        return (await _mediator.Send(new GetDeliveryRunQuery(request.EnhancementRequestId), cancellationToken))!;
    }
}

public sealed record SignUatCommand(Guid EnhancementRequestId, bool Approved, string? Notes) : IRequest<EnhancementDeliveryRunDto>;

public sealed class SignUatCommandHandler : IRequestHandler<SignUatCommand, EnhancementDeliveryRunDto>
{
    private readonly IDeliveryOrchestrationService _orchestration;
    private readonly IDeliveryOrchestrationDispatcher _dispatcher;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SignUatCommandHandler(
        IDeliveryOrchestrationService orchestration,
        IDeliveryOrchestrationDispatcher dispatcher,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _orchestration = orchestration;
        _dispatcher = dispatcher;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<EnhancementDeliveryRunDto> Handle(SignUatCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        await _orchestration.SignUatAsync(
            request.EnhancementRequestId,
            _currentUser.UserId.Value,
            request.Approved,
            request.Notes,
            cancellationToken);

        if (request.Approved)
        {
            _dispatcher.EnqueueProcessing(request.EnhancementRequestId);
        }

        return (await _mediator.Send(new GetDeliveryRunQuery(request.EnhancementRequestId), cancellationToken))!;
    }
}

public sealed record AdvanceDeliveryPastPrCommand(Guid EnhancementRequestId) : IRequest<EnhancementDeliveryRunDto>;

public sealed class AdvanceDeliveryPastPrCommandHandler : IRequestHandler<AdvanceDeliveryPastPrCommand, EnhancementDeliveryRunDto>
{
    private readonly IDeliveryOrchestrationService _orchestration;
    private readonly IDeliveryOrchestrationDispatcher _dispatcher;
    private readonly IMediator _mediator;

    public AdvanceDeliveryPastPrCommandHandler(
        IDeliveryOrchestrationService orchestration,
        IDeliveryOrchestrationDispatcher dispatcher,
        IMediator mediator)
    {
        _orchestration = orchestration;
        _dispatcher = dispatcher;
        _mediator = mediator;
    }

    public async Task<EnhancementDeliveryRunDto> Handle(
        AdvanceDeliveryPastPrCommand request,
        CancellationToken cancellationToken)
    {
        await _orchestration.AdvancePastPullRequestReviewAsync(request.EnhancementRequestId, cancellationToken);
        _dispatcher.EnqueueProcessing(request.EnhancementRequestId);

        return (await _mediator.Send(new GetDeliveryRunQuery(request.EnhancementRequestId), cancellationToken))!;
    }
}

public interface IDeliveryApprovalHook
{
    Task TryStartAfterApprovalAsync(Guid requestId, CancellationToken cancellationToken);
}

public sealed class DeliveryApprovalHook : IDeliveryApprovalHook
{
    private readonly IDeliveryOrchestrationService _orchestration;
    private readonly IDeliveryOrchestrationDispatcher _dispatcher;
    private readonly IEnhancementHubDbContext _dbContext;

    public DeliveryApprovalHook(
        IDeliveryOrchestrationService orchestration,
        IDeliveryOrchestrationDispatcher dispatcher,
        IEnhancementHubDbContext dbContext)
    {
        _orchestration = orchestration;
        _dispatcher = dispatcher;
        _dbContext = dbContext;
    }

    public async Task TryStartAfterApprovalAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request?.TargetApplicationId is null)
        {
            return;
        }

        var tenantId = await (
            from app in _dbContext.Applications.AsNoTracking()
            join team in _dbContext.Teams.AsNoTracking() on app.OwnerTeamId equals team.Id
            where app.Id == request.TargetApplicationId
            select team.TenantId).FirstOrDefaultAsync(cancellationToken);

        if (tenantId == Guid.Empty)
        {
            return;
        }

        var autoStart = await _dbContext.TenantDeliveryProfiles.AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Select(p => p.AutoImplementOnApprove)
            .FirstOrDefaultAsync(cancellationToken);

        if (!autoStart)
        {
            return;
        }

        try
        {
            await _orchestration.StartDeliveryRunAsync(requestId, cancellationToken);
            _dispatcher.EnqueueProcessing(requestId);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
