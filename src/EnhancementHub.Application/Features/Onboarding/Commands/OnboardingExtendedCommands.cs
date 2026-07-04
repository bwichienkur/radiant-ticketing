using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Onboarding;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed record CloneGitRepositoryCommand(
    Guid ApplicationId,
    string RepositoryName,
    string RepositoryUrl,
    string DefaultBranch = "main",
    string? AccessToken = null) : IRequest<GitCloneRequestDto>;

public sealed class CloneGitRepositoryCommandValidator : AbstractValidator<CloneGitRepositoryCommand>
{
    public CloneGitRepositoryCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RepositoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RepositoryUrl).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CloneGitRepositoryCommandHandler
    : IRequestHandler<CloneGitRepositoryCommand, GitCloneRequestDto>
{
    private readonly IGitRepositoryCloneService _cloneService;
    private readonly IMediator _mediator;

    public CloneGitRepositoryCommandHandler(IGitRepositoryCloneService cloneService, IMediator mediator)
    {
        _cloneService = cloneService;
        _mediator = mediator;
    }

    public async Task<GitCloneRequestDto> Handle(
        CloneGitRepositoryCommand request,
        CancellationToken cancellationToken)
    {
        var clone = await _cloneService.CloneAsync(
            request.RepositoryUrl,
            request.DefaultBranch,
            request.AccessToken,
            cancellationToken);

        if (!clone.Succeeded || string.IsNullOrWhiteSpace(clone.LocalPath))
        {
            return new GitCloneRequestDto(false, null, clone.ErrorMessage);
        }

        await _mediator.Send(new RegisterRepositoryCommand(
            request.ApplicationId,
            request.RepositoryName.Trim(),
            clone.LocalPath,
            ExternalTicketProvider.GitHub,
            request.DefaultBranch), cancellationToken);

        return new GitCloneRequestDto(true, clone.LocalPath, null);
    }
}

public sealed record QueueApplicationDiscoveryCommand(
    Guid ApplicationId,
    Guid OnboardingSessionId) : IRequest<OnboardingSessionDto>;

public sealed class QueueApplicationDiscoveryCommandHandler
    : IRequestHandler<QueueApplicationDiscoveryCommand, OnboardingSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public QueueApplicationDiscoveryCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingSessionDto> Handle(
        QueueApplicationDiscoveryCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .FirstOrDefaultAsync(s => s.Id == request.OnboardingSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OnboardingSession), request.OnboardingSessionId);

        session.DiscoveryJobState = DiscoveryJobState.Queued;
        session.DiscoveryStatus = "Queued for discovery...";
        session.LastError = null;
        session.DiscoveryCompletedAt = null;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return OnboardingSessionMapper.ToDto(session);
    }
}

public sealed record SetupOnPremAgentForOnboardingCommand(
    Guid SessionId,
    Guid ApplicationId,
    string ConnectionName,
    DatabaseProviderType Provider) : IRequest<OnPremAgentSetupDto>;

public sealed class SetupOnPremAgentForOnboardingCommandHandler
    : IRequestHandler<SetupOnPremAgentForOnboardingCommand, OnPremAgentSetupDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public SetupOnPremAgentForOnboardingCommandHandler(
        IEnhancementHubDbContext dbContext,
        IMediator mediator,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _configuration = configuration;
    }

    public async Task<OnPremAgentSetupDto> Handle(
        SetupOnPremAgentForOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OnboardingSession), request.SessionId);

        var application = session.Application
            ?? await _dbContext.Applications.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Application), request.ApplicationId);

        var connection = await _mediator.Send(new RegisterDatabaseConnectionCommand(
            request.ApplicationId,
            request.ConnectionName.Trim(),
            request.Provider,
            "Server=on-prem-agent;Database=pending;",
            true), cancellationToken);

        var agent = await _mediator.Send(new RegisterOnPremAgentCommand(
            $"{application.Name} On-Prem Agent",
            $"Registered during onboarding for {application.Name}"), cancellationToken);

        session.OnPremAgentId = agent.Id;
        session.OnPremConnectionId = connection.Id;
        session.SkipDatabase = false;
        session.CurrentStep = OnboardingStep.RunDiscovery;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var apiBaseUrl = _configuration["Onboarding:ApiBaseUrl"] ?? "http://localhost:5075";
        var snippet = $"""
            Agent__ApiBaseUrl={apiBaseUrl}
            Agent__AgentId={agent.Id}
            Agent__ConnectionId={connection.Id}
            Agent__ConnectionString=<your-on-prem-connection-string>
            Agent__Provider={request.Provider}
            """;

        var runCommand = $"dotnet run --project src/EnhancementHub.Agent";

        return new OnPremAgentSetupDto(
            agent.Id,
            connection.Id,
            connection.Name,
            apiBaseUrl,
            snippet,
            runCommand);
    }
}

public sealed record BuildDatabaseConnectionStringQuery(
    DatabaseProviderType Provider,
    string Host,
    int Port,
    string Database,
    string? Username,
    string? Password,
    bool IntegratedSecurity = false) : IRequest<DatabaseConnectionStringDto>;

public sealed class BuildDatabaseConnectionStringQueryHandler
    : IRequestHandler<BuildDatabaseConnectionStringQuery, DatabaseConnectionStringDto>
{
    public Task<DatabaseConnectionStringDto> Handle(
        BuildDatabaseConnectionStringQuery request,
        CancellationToken cancellationToken)
    {
        var connectionString = request.Provider switch
        {
            DatabaseProviderType.PostgreSQL =>
                $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password};",
            DatabaseProviderType.SqlServer when request.IntegratedSecurity =>
                $"Server={request.Host},{request.Port};Database={request.Database};Trusted_Connection=True;TrustServerCertificate=True;",
            DatabaseProviderType.SqlServer =>
                $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;",
            _ => $"Data Source={request.Database}"
        };

        return Task.FromResult(new DatabaseConnectionStringDto(connectionString));
    }
}

public sealed record UploadRepositoryArchiveCommand(
    Guid ApplicationId,
    string RepositoryName,
    Stream ArchiveStream) : IRequest<GitCloneRequestDto>;

public sealed class UploadRepositoryArchiveCommandValidator : AbstractValidator<UploadRepositoryArchiveCommand>
{
    public UploadRepositoryArchiveCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RepositoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ArchiveStream).NotNull();
    }
}

public sealed class UploadRepositoryArchiveCommandHandler
    : IRequestHandler<UploadRepositoryArchiveCommand, GitCloneRequestDto>
{
    private readonly IRepositoryArchiveExtractService _archiveExtractService;
    private readonly IMediator _mediator;

    public UploadRepositoryArchiveCommandHandler(
        IRepositoryArchiveExtractService archiveExtractService,
        IMediator mediator)
    {
        _archiveExtractService = archiveExtractService;
        _mediator = mediator;
    }

    public async Task<GitCloneRequestDto> Handle(
        UploadRepositoryArchiveCommand request,
        CancellationToken cancellationToken)
    {
        var extracted = await _archiveExtractService.ExtractZipAsync(request.ArchiveStream, cancellationToken);
        if (!extracted.Succeeded || string.IsNullOrWhiteSpace(extracted.LocalPath))
        {
            return new GitCloneRequestDto(false, null, extracted.ErrorMessage);
        }

        await _mediator.Send(new RegisterRepositoryCommand(
            request.ApplicationId,
            request.RepositoryName.Trim(),
            extracted.LocalPath,
            ExternalTicketProvider.GitHub,
            "main"), cancellationToken);

        return new GitCloneRequestDto(true, extracted.LocalPath, null);
    }
}

public sealed record CloneGitHubAppRepositoryCommand(
    Guid ApplicationId,
    string RepositoryName,
    string Owner,
    string Repository,
    string DefaultBranch = "main",
    long? InstallationId = null) : IRequest<GitCloneRequestDto>;

public sealed class CloneGitHubAppRepositoryCommandValidator : AbstractValidator<CloneGitHubAppRepositoryCommand>
{
    public CloneGitHubAppRepositoryCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RepositoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Repository).NotEmpty().MaximumLength(200);
    }
}

public sealed class CloneGitHubAppRepositoryCommandHandler
    : IRequestHandler<CloneGitHubAppRepositoryCommand, GitCloneRequestDto>
{
    private readonly IGitHubAppCloneService _gitHubAppCloneService;
    private readonly IMediator _mediator;

    public CloneGitHubAppRepositoryCommandHandler(
        IGitHubAppCloneService gitHubAppCloneService,
        IMediator mediator)
    {
        _gitHubAppCloneService = gitHubAppCloneService;
        _mediator = mediator;
    }

    public async Task<GitCloneRequestDto> Handle(
        CloneGitHubAppRepositoryCommand request,
        CancellationToken cancellationToken)
    {
        var clone = await _gitHubAppCloneService.CloneRepositoryAsync(
            request.Owner,
            request.Repository,
            request.DefaultBranch,
            request.InstallationId,
            cancellationToken);

        if (!clone.Succeeded || string.IsNullOrWhiteSpace(clone.LocalPath))
        {
            return new GitCloneRequestDto(false, null, clone.ErrorMessage);
        }

        await _mediator.Send(new RegisterRepositoryCommand(
            request.ApplicationId,
            request.RepositoryName.Trim(),
            clone.LocalPath,
            ExternalTicketProvider.GitHub,
            request.DefaultBranch), cancellationToken);

        return new GitCloneRequestDto(true, clone.LocalPath, null);
    }
}

public sealed record GetGitHubAppStatusQuery : IRequest<GitHubAppStatusDto>;

public sealed record GitHubAppStatusDto(bool IsConfigured, long? DefaultInstallationId);

public sealed class GetGitHubAppStatusQueryHandler : IRequestHandler<GetGitHubAppStatusQuery, GitHubAppStatusDto>
{
    private readonly IGitHubAppCloneService _gitHubAppCloneService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public GetGitHubAppStatusQueryHandler(
        IGitHubAppCloneService gitHubAppCloneService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _gitHubAppCloneService = gitHubAppCloneService;
        _configuration = configuration;
    }

    public Task<GitHubAppStatusDto> Handle(GetGitHubAppStatusQuery request, CancellationToken cancellationToken)
    {
        var installation = _configuration["GitHubApp:InstallationId"];
        long? defaultInstallation = long.TryParse(installation, out var parsed) ? parsed : null;
        return Task.FromResult(new GitHubAppStatusDto(_gitHubAppCloneService.IsConfigured, defaultInstallation));
    }
}
