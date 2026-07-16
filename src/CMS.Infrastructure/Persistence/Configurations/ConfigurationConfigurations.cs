using CMS.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.Property(s => s.Key).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Group).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(512);
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
    }
}

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");
        builder.Property(f => f.Name).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(512);
        builder.Property(f => f.Environment).HasMaxLength(64);
        builder.HasIndex(f => new { f.TenantId, f.Name }).IsUnique();
    }
}

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.Property(k => k.Name).HasMaxLength(128).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(16).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(k => k.Scopes).HasMaxLength(1024);
        builder.HasIndex(k => k.Prefix).IsUnique();
        builder.Ignore(k => k.IsExpired);

        builder.HasOne(k => k.Owner)
            .WithMany()
            .HasForeignKey(k => k.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
