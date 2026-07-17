using CMS.Domain.Authorization;

namespace CMS.UnitTests;

public class PermissionRegistryTests
{
    [Fact]
    public void All_contains_a_core_permission()
        => Assert.Contains(PermissionRegistry.All, p => p.Name == Permissions.Content.Edit);

    [Fact]
    public void All_contains_the_sample_module_permission()
        => Assert.Contains(PermissionRegistry.All, p => p.Name == Permissions.Announcements.View);

    [Fact]
    public void All_has_no_duplicate_names()
    {
        var names = PermissionRegistry.All.Select(p => p.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void Every_permission_has_a_group_and_display_name()
        => Assert.All(PermissionRegistry.All, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Group));
            Assert.False(string.IsNullOrWhiteSpace(p.DisplayName));
        });
}
