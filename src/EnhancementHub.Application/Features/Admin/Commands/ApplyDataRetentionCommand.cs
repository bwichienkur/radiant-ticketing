using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Commands;

public sealed record ApplyDataRetentionCommand(bool DryRun = false) : IRequest<DataRetentionResultDto>;
