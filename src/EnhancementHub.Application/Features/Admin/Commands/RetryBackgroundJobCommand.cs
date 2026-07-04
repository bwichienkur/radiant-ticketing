using MediatR;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record RetryBackgroundJobCommand(string JobId) : IRequest<bool>;
