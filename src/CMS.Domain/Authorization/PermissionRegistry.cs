using System.Reflection;

namespace CMS.Domain.Authorization;

/// <summary>
/// Metadata describing a single permission, used for seeding the Permissions
/// table and rendering the permission-assignment UI.
/// </summary>
/// <param name="Name">The stable identifier, e.g. "Content.Edit".</param>
/// <param name="Group">The logical group for UI grouping, e.g. "Content".</param>
/// <param name="DisplayName">A human-friendly label, e.g. "Edit content".</param>
public sealed record PermissionDefinition(string Name, string Group, string DisplayName);

/// <summary>
/// Discovers every permission constant declared on <see cref="Permissions"/> via
/// reflection so the catalogue is defined in exactly one place. Used by the
/// database seeder and permission-management screens.
/// </summary>
public static class PermissionRegistry
{
    private static readonly IReadOnlyList<PermissionDefinition> _all = Discover();

    /// <summary>All known permissions, ordered by group then name.</summary>
    public static IReadOnlyList<PermissionDefinition> All => _all;

    private static List<PermissionDefinition> Discover()
    {
        var definitions = new List<PermissionDefinition>();

        foreach (var group in typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var field in group.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (field is { IsLiteral: true, IsInitOnly: false } && field.GetRawConstantValue() is string value)
                {
                    var groupName = value.Contains('.', StringComparison.Ordinal)
                        ? value[..value.IndexOf('.', StringComparison.Ordinal)]
                        : group.Name;

                    definitions.Add(new PermissionDefinition(value, groupName, Humanize(value)));
                }
            }
        }

        return definitions
            .OrderBy(d => d.Group, StringComparer.Ordinal)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Turns "Content.Edit" into "Edit content" for display.</summary>
    private static string Humanize(string permission)
    {
        var parts = permission.Split('.');
        if (parts.Length != 2)
        {
            return permission;
        }

        var action = SplitPascalCase(parts[1]);
        return $"{action} {parts[0].ToLowerInvariant()}";
    }

    private static string SplitPascalCase(string value)
    {
        var chars = new List<char>();
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]))
            {
                chars.Add(' ');
            }

            chars.Add(i == 0 ? value[i] : char.ToLowerInvariant(value[i]));
        }

        return new string([.. chars]);
    }
}
