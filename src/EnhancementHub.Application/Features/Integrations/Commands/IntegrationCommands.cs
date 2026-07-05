using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Integrations.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Integrations.Commands;

public sealed record RegisterOpenApiSpecCommand(
    Guid ApplicationId,
    string Name,
    string SpecDocument) : IRequest<OpenApiRegistrationDto>;

public sealed class RegisterOpenApiSpecCommandValidator : AbstractValidator<RegisterOpenApiSpecCommand>
{
    public RegisterOpenApiSpecCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SpecDocument).NotEmpty();
    }
}

public sealed class RegisterOpenApiSpecCommandHandler
    : IRequestHandler<RegisterOpenApiSpecCommand, OpenApiRegistrationDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;
    private readonly IOpenApiIngestionService _ingestionService;

    public RegisterOpenApiSpecCommandHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService,
        IOpenApiIngestionService ingestionService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _ingestionService = ingestionService;
    }

    public async Task<OpenApiRegistrationDto> Handle(
        RegisterOpenApiSpecCommand request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var validation = await _ingestionService.ParseAndValidateAsync(request.SpecDocument, cancellationToken);
        if (!validation.Succeeded)
        {
            throw new ValidationException(validation.ErrorMessage ?? "Invalid OpenAPI document.");
        }

        var now = DateTime.UtcNow;
        var registration = new Domain.Entities.OpenApiRegistration
        {
            Id = Guid.NewGuid(),
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            SpecDocument = request.SpecDocument,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.OpenApiRegistrations.Add(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ingest = await _ingestionService.IngestAsync(registration.Id, cancellationToken);
        return ToDto(registration, ingest.EndpointCount);
    }

    internal static OpenApiRegistrationDto ToDto(Domain.Entities.OpenApiRegistration registration, int endpointCount) =>
        new(
            registration.Id,
            registration.ApplicationId,
            registration.Name,
            endpointCount,
            registration.BaseUrl,
            registration.LastIngestedAt);
}

public sealed record IngestPolyglotSymbolsCommand(
    Guid RepositoryId,
    string Language,
    IReadOnlyList<PolyglotSymbolInput> Symbols) : IRequest<PolyglotIngestionResult>;

public sealed class IngestPolyglotSymbolsCommandHandler
    : IRequestHandler<IngestPolyglotSymbolsCommand, PolyglotIngestionResult>
{
    private readonly IPolyglotSymbolIngestionService _ingestionService;

    public IngestPolyglotSymbolsCommandHandler(IPolyglotSymbolIngestionService ingestionService) =>
        _ingestionService = ingestionService;

    public Task<PolyglotIngestionResult> Handle(
        IngestPolyglotSymbolsCommand request,
        CancellationToken cancellationToken) =>
        _ingestionService.IngestAsync(
            request.RepositoryId,
            request.Symbols,
            request.Language,
            cancellationToken);
}
