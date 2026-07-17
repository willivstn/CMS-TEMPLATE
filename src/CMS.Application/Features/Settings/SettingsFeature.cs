using CMS.Application.Common.Abstractions;
using CMS.Domain.Enums;
using CMS.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Settings;

// ---- DTOs ----------------------------------------------------------------

public sealed record SettingItemDto(string Key, string? Value, SettingType Type, string? Description, bool IsEncrypted, bool IsPublic);

public sealed record SettingGroupDto(string Group, IReadOnlyList<SettingItemDto> Items);

// ---- Query ---------------------------------------------------------------

public sealed record GetSettingsQuery : IRequest<IReadOnlyList<SettingGroupDto>>;

public sealed class GetSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSettingsQuery, IReadOnlyList<SettingGroupDto>>
{
    public async Task<IReadOnlyList<SettingGroupDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await db.Settings.AsNoTracking()
            .OrderBy(s => s.Group).ThenBy(s => s.Key)
            .Select(s => new { s.Key, s.Value, s.Type, s.Group, s.Description, s.IsEncrypted, s.IsPublic })
            .ToListAsync(cancellationToken);

        return settings
            .GroupBy(s => s.Group)
            .Select(g => new SettingGroupDto(g.Key, g.Select(s =>
                // Never surface encrypted values in the editor.
                new SettingItemDto(s.Key, s.IsEncrypted ? null : s.Value, s.Type, s.Description, s.IsEncrypted, s.IsPublic)).ToList()))
            .ToList();
    }
}

// ---- Command -------------------------------------------------------------

/// <summary>Updates setting values. Blank values for encrypted settings are ignored (keep existing).</summary>
public sealed record UpdateSettingsCommand(IReadOnlyDictionary<string, string?> Values) : IRequest<Result>;

public sealed class UpdateSettingsCommandHandler(
    IApplicationDbContext db, IEncryptionService encryption, ICacheService cache)
    : IRequestHandler<UpdateSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var keys = request.Values.Keys.ToList();
        var settings = await db.Settings.Where(s => keys.Contains(s.Key)).ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            var value = request.Values[setting.Key];

            if (setting.IsEncrypted)
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue; // keep existing secret
                }

                setting.Value = encryption.Encrypt(value);
            }
            else
            {
                setting.Value = setting.Type == SettingType.Boolean
                    ? (value == "true" || value == "on").ToString().ToLowerInvariant()
                    : value;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.RemoveByPrefix("setting:");
        return Result.Success();
    }
}
