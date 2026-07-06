using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Application.Features.CustomFields.Queries;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.CustomFields.Commands;

public sealed record UpsertCustomFieldDefinitionCommand(
    Guid? Id,
    string Key,
    string Label,
    CustomFieldType FieldType,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<string>? Options) : IRequest<CustomFieldDefinitionDto>;

public sealed class UpsertCustomFieldDefinitionCommandValidator : AbstractValidator<UpsertCustomFieldDefinitionCommand>
{
    public UpsertCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9_]+$").WithMessage("Key must be lowercase letters, numbers, or underscores.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FieldType).IsInEnum();
    }
}

public sealed class UpsertCustomFieldDefinitionCommandHandler
    : IRequestHandler<UpsertCustomFieldDefinitionCommand, CustomFieldDefinitionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public UpsertCustomFieldDefinitionCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<CustomFieldDefinitionDto> Handle(
        UpsertCustomFieldDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var key = request.Key.Trim().ToLowerInvariant();
        CustomFieldDefinition entity;
        var now = DateTime.UtcNow;

        if (request.Id.HasValue)
        {
            entity = await _dbContext.CustomFieldDefinitions
                .FirstOrDefaultAsync(d => d.Id == request.Id.Value, cancellationToken)
                ?? throw new Common.Exceptions.NotFoundException(nameof(CustomFieldDefinition), request.Id.Value);
            entity.UpdatedAt = now;
        }
        else
        {
            var exists = await _dbContext.CustomFieldDefinitions
                .AnyAsync(d => d.Key == key, cancellationToken);
            if (exists)
            {
                throw new ValidationException($"Custom field key '{key}' already exists.");
            }

            entity = new CustomFieldDefinition
            {
                Id = Guid.NewGuid(),
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.CustomFieldDefinitions.Add(entity);
        }

        entity.Key = key;
        entity.Label = request.Label.Trim();
        entity.FieldType = request.FieldType;
        entity.IsRequired = request.IsRequired;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.OptionsJson = request.FieldType == CustomFieldType.Select
            ? JsonSerializer.Serialize(request.Options ?? [])
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CustomFieldQueries.ToDto(entity);
    }
}

public sealed record DeleteCustomFieldDefinitionCommand(Guid Id) : IRequest;

public sealed class DeleteCustomFieldDefinitionCommandHandler
    : IRequestHandler<DeleteCustomFieldDefinitionCommand>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public DeleteCustomFieldDefinitionCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task Handle(DeleteCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(CustomFieldDefinition), request.Id);

        _dbContext.CustomFieldDefinitions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal static class CustomFieldValueWriter
{
    public static async Task SaveValuesAsync(
        IEnhancementHubDbContext dbContext,
        Guid requestId,
        IReadOnlyList<CustomFieldValueInput>? values,
        CancellationToken cancellationToken)
    {
        if (values is null || values.Count == 0)
        {
            return;
        }

        var definitions = await dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var byKey = definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        var existing = await dbContext.EnhancementRequestCustomFieldValues
            .Where(v => v.EnhancementRequestId == requestId)
            .ToListAsync(cancellationToken);

        foreach (var input in values)
        {
            if (!byKey.TryGetValue(input.Key, out var definition))
            {
                continue;
            }

            ValidateValue(definition, input);

            var entity = existing.FirstOrDefault(v => v.CustomFieldDefinitionId == definition.Id);
            var now = DateTime.UtcNow;
            if (entity is null)
            {
                entity = new EnhancementRequestCustomFieldValue
                {
                    Id = Guid.NewGuid(),
                    EnhancementRequestId = requestId,
                    CustomFieldDefinitionId = definition.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                dbContext.EnhancementRequestCustomFieldValues.Add(entity);
            }
            else
            {
                entity.UpdatedAt = now;
            }

            entity.TextValue = definition.FieldType is CustomFieldType.Text or CustomFieldType.Select
                ? input.TextValue
                : null;
            entity.NumberValue = definition.FieldType == CustomFieldType.Number ? input.NumberValue : null;
            entity.DateValue = definition.FieldType == CustomFieldType.Date ? input.DateValue : null;
            entity.UserValueId = definition.FieldType == CustomFieldType.User ? input.UserValueId : null;
        }

        foreach (var definition in definitions.Where(d => d.IsRequired))
        {
            var provided = values.Any(v =>
                string.Equals(v.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
            if (!provided && !existing.Any(v => v.CustomFieldDefinitionId == definition.Id))
            {
                throw new ValidationException($"Required custom field '{definition.Label}' is missing.");
            }
        }
    }

    private static void ValidateValue(CustomFieldDefinition definition, CustomFieldValueInput input)
    {
        switch (definition.FieldType)
        {
            case CustomFieldType.Text or CustomFieldType.Select:
                if (string.IsNullOrWhiteSpace(input.TextValue))
                {
                    throw new ValidationException($"Custom field '{definition.Label}' requires a text value.");
                }

                break;
            case CustomFieldType.Number when input.NumberValue is null:
                throw new ValidationException($"Custom field '{definition.Label}' requires a number.");
            case CustomFieldType.Date when input.DateValue is null:
                throw new ValidationException($"Custom field '{definition.Label}' requires a date.");
            case CustomFieldType.User when input.UserValueId is null:
                throw new ValidationException($"Custom field '{definition.Label}' requires a user.");
        }
    }
}
