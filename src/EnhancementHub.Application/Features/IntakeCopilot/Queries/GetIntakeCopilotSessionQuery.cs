using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.IntakeCopilot.Commands;
using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.IntakeCopilot.Queries;

public sealed record GetIntakeCopilotSessionQuery(Guid SessionId) : IRequest<IntakeCopilotSessionDto>;

public sealed class GetIntakeCopilotSessionQueryHandler
    : IRequestHandler<GetIntakeCopilotSessionQuery, IntakeCopilotSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetIntakeCopilotSessionQueryHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IntakeCopilotSessionDto> Handle(
        GetIntakeCopilotSessionQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new ForbiddenException("Authentication required.");
        }

        var session = await _dbContext.IntakeCopilotSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException("IntakeCopilotSession", request.SessionId);

        if (session.UserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this intake session.");
        }

        return IntakeCopilotMapper.ToSessionDto(session);
    }
}
