using CMS.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CMS.Infrastructure.Configuration;

/// <summary>
/// Evaluates feature flags with read-through caching. A flag scoped to an
/// environment only counts as enabled when that environment is current.
/// </summary>
public sealed class FeatureService(
    IApplicationDbContext db, ICacheService cache, IHostEnvironment environment) : IFeatureService
{
    public Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateAsync($"feature:{name}", async ct =>
        {
            var flag = await db.FeatureFlags.AsNoTracking()
                .Where(f => f.Name == name)
                .Select(f => new { f.IsEnabled, f.Environment })
                .FirstOrDefaultAsync(ct);

            if (flag is null || !flag.IsEnabled)
            {
                return false;
            }

            return string.IsNullOrEmpty(flag.Environment)
                || string.Equals(flag.Environment, environment.EnvironmentName, StringComparison.OrdinalIgnoreCase);
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }
}
