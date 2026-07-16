using CMS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ApplicationUser"/> with friendly table name, profile fields and navigations.</summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.FirstName).HasMaxLength(128);
        builder.Property(u => u.LastName).HasMaxLength(128);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.TimeZoneId).HasMaxLength(64);
        builder.Property(u => u.LanguageCode).HasMaxLength(16);
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.ModifiedBy).HasMaxLength(256);
        builder.Property(u => u.DeletedBy).HasMaxLength(256);

        builder.HasIndex(u => u.IsDeleted);

        builder.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).IsRequired();
        builder.HasMany(u => u.UserPermissions).WithOne(up => up.User).HasForeignKey(up => up.UserId).IsRequired();
        builder.HasMany(u => u.RefreshTokens).WithOne(t => t.User).HasForeignKey(t => t.UserId).IsRequired();
        builder.HasMany(u => u.LoginHistories).WithOne(l => l.User).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Maps <see cref="ApplicationRole"/> with friendly table name and permission navigations.</summary>
public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        builder.Property(r => r.Description).HasMaxLength(512);
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.ModifiedBy).HasMaxLength(256);

        builder.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId).IsRequired();
        builder.HasMany(r => r.RolePermissions).WithOne(rp => rp.Role).HasForeignKey(rp => rp.RoleId).IsRequired();
    }
}

/// <summary>Renames the remaining Identity tables and wires optional navigations.</summary>
public sealed class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder) => builder.ToTable("UserRoles");
}

public sealed class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<ApplicationUserClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder)
    {
        builder.ToTable("UserClaims");
        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).IsRequired();
    }
}

public sealed class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
    {
        builder.ToTable("UserLogins");
        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).IsRequired();
    }
}

public sealed class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder)
    {
        builder.ToTable("UserTokens");
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).IsRequired();
    }
}

public sealed class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("RoleClaims");
        builder.HasOne(c => c.Role).WithMany().HasForeignKey(c => c.RoleId).IsRequired();
    }
}

/// <summary>Maps sign-in history.</summary>
public sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories");
        builder.Property(l => l.UserName).HasMaxLength(256).IsRequired();
        builder.Property(l => l.FailureReason).HasMaxLength(256);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(512);
        builder.HasIndex(l => l.TimestampUtc);
        builder.HasIndex(l => l.UserId);
    }
}

/// <summary>Maps refresh tokens for API authentication.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash);
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsActive);
    }
}
