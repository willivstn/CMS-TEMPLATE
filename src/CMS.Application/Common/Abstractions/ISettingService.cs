namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Typed access to database-driven application settings, cached for the request
/// and invalidated on write. Encrypted settings are transparently decrypted.
/// </summary>
public interface ISettingService
{
    /// <summary>Gets a setting converted to <typeparamref name="T"/>, or <paramref name="fallback"/> if absent.</summary>
    Task<T> GetAsync<T>(string key, T fallback, CancellationToken cancellationToken = default);

    /// <summary>Gets a raw setting value, or null if absent.</summary>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a setting value.</summary>
    Task SetAsync(string key, string? value, CancellationToken cancellationToken = default);
}
