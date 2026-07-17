using System.Linq.Expressions;
using System.Reflection;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Common;
using CMS.Domain.Entities.Access;
using CMS.Domain.Entities.Communication;
using CMS.Domain.Entities.Configuration;
using CMS.Domain.Entities.Content;
using CMS.Domain.Entities.Identity;
using CMS.Domain.Entities.Localization;
using CMS.Domain.Entities.Media;
using CMS.Domain.Entities.Navigation;
using CMS.Domain.Entities.Notifications;
using CMS.Domain.Entities.System;
using CMS.Domain.Entities.Tenancy;
using CMS.Domain.Entities.Theming;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence;

/// <summary>
/// The application's EF Core context. Extends Identity with the CMS domain model
/// and implements <see cref="IApplicationDbContext"/> so the Application layer
/// depends only on the abstraction. Global query filters transparently exclude
/// soft-deleted rows and scope tenant-owned entities to the current tenant.
/// </summary>
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<
        ApplicationUser, ApplicationRole, Guid,
        ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin,
        ApplicationRoleClaim, ApplicationUserToken>(options), IApplicationDbContext
{
    /// <summary>Current tenant, referenced by tenant query filters (re-read per query).</summary>
    public Guid CurrentTenantId { get; } = tenantContext.CurrentTenantId;

    /// <summary>Whether tenant filtering is active, referenced by query filters.</summary>
    public bool IsMultiTenantEnabled { get; } = tenantContext.IsMultiTenantEnabled;

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemPermission> MenuItemPermissions => Set<MenuItemPermission>();

    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentVersion> ContentVersions => Set<ContentVersion>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();

    public DbSet<MediaFolder> MediaFolders => Set<MediaFolder>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Translation> Translations => Set<Translation>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();
    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Theme> Themes => Set<Theme>();

    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Domain.Entities.Modules.Announcement> Announcements => Set<Domain.Entities.Modules.Announcement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Pick up every IEntityTypeConfiguration<T> in this assembly.
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyGlobalFilters(builder);
    }

    /// <summary>
    /// Applies soft-delete and tenant query filters to every entity that opts in
    /// via <see cref="ISoftDeletable"/> / <see cref="ITenantEntity"/>, combining
    /// them into a single predicate per entity type.
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var softDelete = typeof(ISoftDeletable).IsAssignableFrom(clrType);
            var tenant = typeof(ITenantEntity).IsAssignableFrom(clrType);

            if (!softDelete && !tenant)
            {
                continue;
            }

            var filter = typeof(AppDbContext)
                .GetMethod(nameof(BuildQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(clrType)
                .Invoke(this, [softDelete, tenant]);

            entityType.SetQueryFilter((LambdaExpression)filter!);
        }
    }

    private LambdaExpression BuildQueryFilter<TEntity>(bool softDelete, bool tenant)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? body = null;

        if (softDelete)
        {
            // e => !EF.Property<bool>(e, "IsDeleted")
            var isDeleted = Expression.Call(
                typeof(EF), nameof(EF.Property), [typeof(bool)],
                parameter, Expression.Constant(nameof(ISoftDeletable.IsDeleted)));
            body = Expression.Not(isDeleted);
        }

        if (tenant)
        {
            // e => !IsMultiTenantEnabled || EF.Property<Guid>(e, "TenantId") == CurrentTenantId
            var tenantId = Expression.Call(
                typeof(EF), nameof(EF.Property), [typeof(Guid)],
                parameter, Expression.Constant(nameof(ITenantEntity.TenantId)));

            var multiTenantDisabled = Expression.Not(
                Expression.Property(Expression.Constant(this), nameof(IsMultiTenantEnabled)));
            var matchesTenant = Expression.Equal(
                tenantId,
                Expression.Property(Expression.Constant(this), nameof(CurrentTenantId)));

            var tenantFilter = Expression.OrElse(multiTenantDisabled, matchesTenant);
            body = body is null ? tenantFilter : Expression.AndAlso(body, tenantFilter);
        }

        return Expression.Lambda(body!, parameter);
    }
}
