namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Computes a user's <em>effective</em> permission set:
/// <c>(union of role permissions) + explicit user grants − explicit user denies</c>.
/// Members of the built-in Administrator role implicitly hold every permission.
/// The result is materialised into claims at sign-in, so per-request authorization
/// reads claims rather than hitting the database.
/// </summary>
public interface IPermissionService
{
    /// <summary>Returns the distinct permission names the user effectively holds.</summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the user effectively holds <paramref name="permission"/>.</summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
}
