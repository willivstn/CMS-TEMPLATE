using CMS.Domain.Entities.Tenancy;
using CMS.Domain.Entities.Theming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Identifier).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Host).HasMaxLength(256);
        builder.Property(t => t.ThemeKey).HasMaxLength(64);
        builder.HasIndex(t => t.Identifier).IsUnique();
        builder.HasIndex(t => t.Host);
    }
}

public sealed class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder.ToTable("Themes");
        builder.Property(t => t.Key).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Variables).HasMaxLength(4000);
        builder.HasIndex(t => t.Key).IsUnique();
    }
}
