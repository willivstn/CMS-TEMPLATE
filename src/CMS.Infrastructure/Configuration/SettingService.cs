using System.Globalization;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Configuration;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Configuration;

/// <summary>
/// Reads/writes database settings with read-through caching. Encrypted settings
/// are decrypted on read and encrypted on write via <see cref="IEncryptionService"/>.
/// </summary>
public sealed class SettingService(
    IApplicationDbContext db, ICacheService cache, IEncryptionService encryption) : ISettingService
{
    private const string CachePrefix = "setting:";

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync($"{CachePrefix}{key}", async ct =>
        {
            var setting = await db.Settings.AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => new { s.Value, s.IsEncrypted })
                .FirstOrDefaultAsync(ct);

            if (setting?.Value is null)
            {
                return null;
            }

            return setting.IsEncrypted ? encryption.Decrypt(setting.Value) : setting.Value;
        }, TimeSpan.FromMinutes(30), cancellationToken);
    }

    public async Task<T> GetAsync<T>(string key, T fallback, CancellationToken cancellationToken = default)
    {
        var raw = await GetStringAsync(key, cancellationToken);
        if (raw is null)
        {
            return fallback;
        }

        try
        {
            var target = typeof(T);
            if (target == typeof(string))
            {
                return (T)(object)raw;
            }

            if (target == typeof(bool))
            {
                return (T)(object)bool.Parse(raw);
            }

            if (target == typeof(int))
            {
                return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);
            }

            return (T)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return fallback;
        }
    }

    public async Task SetAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new Setting { Id = Guid.NewGuid(), Key = key, Type = SettingType.String, Group = "General" };
            db.Settings.Add(setting);
        }

        setting.Value = setting.IsEncrypted && value is not null ? encryption.Encrypt(value) : value;

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove($"{CachePrefix}{key}");
    }
}
