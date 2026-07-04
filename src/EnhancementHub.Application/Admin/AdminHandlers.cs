using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Models;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Admin;

public sealed record GetSystemSettingsQuery(string? Category = null) : IRequest<IReadOnlyList<SystemSettingDto>>;

public sealed class GetSystemSettingsQueryHandler : IRequestHandler<GetSystemSettingsQuery, IReadOnlyList<SystemSettingDto>>
{
    private readonly IEnhancementHubDbContext _db;

    public GetSystemSettingsQueryHandler(IEnhancementHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<SystemSettingDto>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.SystemSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(s => s.Category == request.Category);

        return await query.OrderBy(s => s.Category).ThenBy(s => s.Key)
            .Select(s => new SystemSettingDto(s.Id, s.Key, s.Value, s.Category, s.Description))
            .ToListAsync(cancellationToken);
    }
}

public sealed record UpdateSystemSettingCommand(Guid Id, string Value) : IRequest<bool>;

public sealed class UpdateSystemSettingCommandHandler : IRequestHandler<UpdateSystemSettingCommand, bool>
{
    private readonly IEnhancementHubDbContext _db;
    private readonly IAuditService _audit;

    public UpdateSystemSettingCommandHandler(IEnhancementHubDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<bool> Handle(UpdateSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (setting is null) return false;

        var previous = setting.Value;
        setting.Value = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("SettingUpdated", nameof(SystemSetting), setting.Id, $"{setting.Key}: {previous} -> {request.Value}", cancellationToken);
        return true;
    }
}

public sealed record ListAiPromptConfigurationsQuery : IRequest<IReadOnlyList<AiPromptConfigurationDto>>;

public sealed class ListAiPromptConfigurationsQueryHandler : IRequestHandler<ListAiPromptConfigurationsQuery, IReadOnlyList<AiPromptConfigurationDto>>
{
    private readonly IEnhancementHubDbContext _db;

    public ListAiPromptConfigurationsQueryHandler(IEnhancementHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<AiPromptConfigurationDto>> Handle(ListAiPromptConfigurationsQuery request, CancellationToken cancellationToken) =>
        await _db.AiPromptConfigurations.AsNoTracking()
            .OrderBy(p => p.Name).ThenByDescending(p => p.Version)
            .Select(p => new AiPromptConfigurationDto(
                p.Id, p.Name, p.Version, p.SystemPromptTemplate, p.UserPromptTemplate, p.IsActive))
            .ToListAsync(cancellationToken);
}

public sealed record UpdateAiPromptConfigurationCommand(
    Guid Id,
    string SystemPromptTemplate,
    string UserPromptTemplate,
    bool IsActive) : IRequest<bool>;

public sealed class UpdateAiPromptConfigurationCommandHandler : IRequestHandler<UpdateAiPromptConfigurationCommand, bool>
{
    private readonly IEnhancementHubDbContext _db;
    private readonly IAuditService _audit;

    public UpdateAiPromptConfigurationCommandHandler(IEnhancementHubDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<bool> Handle(UpdateAiPromptConfigurationCommand request, CancellationToken cancellationToken)
    {
        var prompt = await _db.AiPromptConfigurations.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (prompt is null) return false;

        prompt.SystemPromptTemplate = request.SystemPromptTemplate;
        prompt.UserPromptTemplate = request.UserPromptTemplate;
        prompt.IsActive = request.IsActive;
        prompt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("PromptUpdated", nameof(Domain.Entities.AiPromptConfiguration), prompt.Id, prompt.Name, cancellationToken);
        return true;
    }
}
