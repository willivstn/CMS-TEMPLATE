namespace CMS.Application.Common.Abstractions;

/// <summary>Runtime feature-flag checks, cached and evaluated against the current environment.</summary>
public interface IFeatureService
{
    /// <summary>True when the named flag is enabled (and applies to the current environment).</summary>
    Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default);
}
