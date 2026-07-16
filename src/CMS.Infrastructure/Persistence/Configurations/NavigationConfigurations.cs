using CMS.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.Property(m => m.Title).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Icon).HasMaxLength(64);
        builder.Property(m => m.Area).HasMaxLength(64);
        builder.Property(m => m.Controller).HasMaxLength(64);
        builder.Property(m => m.Action).HasMaxLength(64);
        builder.Property(m => m.RouteValues).HasMaxLength(512);
        builder.Property(m => m.Url).HasMaxLength(512);

        builder.HasIndex(m => new { m.TenantId, m.ParentId, m.SortOrder });

        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MenuItemPermissionConfiguration : IEntityTypeConfiguration<MenuItemPermission>
{
    public void Configure(EntityTypeBuilder<MenuItemPermission> builder)
    {
        builder.ToTable("MenuItemPermissions");
        builder.HasIndex(mp => new { mp.MenuItemId, mp.PermissionId }).IsUnique();

        builder.HasOne(mp => mp.MenuItem)
            .WithMany(m => m.MenuPermissions)
            .HasForeignKey(mp => mp.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mp => mp.Permission)
            .WithMany()
            .HasForeignKey(mp => mp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
