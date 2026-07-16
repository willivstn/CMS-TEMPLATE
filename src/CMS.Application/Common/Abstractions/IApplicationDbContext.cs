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
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Common.Abstractions;

/// <summary>
/// The persistence gateway exposed to the Application layer. CQRS handlers query
/// and mutate through these sets and call <see cref="SaveChangesAsync"/>; the
/// concrete EF Core context lives in Infrastructure. Identity's own tables
/// (users, roles) are accessed via the Identity managers, not here.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }

    DbSet<MenuItem> MenuItems { get; }
    DbSet<MenuItemPermission> MenuItemPermissions { get; }

    DbSet<Setting> Settings { get; }
    DbSet<FeatureFlag> FeatureFlags { get; }
    DbSet<ApiKey> ApiKeys { get; }

    DbSet<Category> Categories { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<ContentVersion> ContentVersions { get; }
    DbSet<ContentTag> ContentTags { get; }

    DbSet<MediaFolder> MediaFolders { get; }
    DbSet<MediaItem> MediaItems { get; }

    DbSet<Language> Languages { get; }
    DbSet<Translation> Translations { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationRecipient> NotificationRecipients { get; }

    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SystemLog> SystemLogs { get; }
    DbSet<JobRun> JobRuns { get; }
    DbSet<ScheduledTask> ScheduledTasks { get; }

    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailMessage> EmailMessages { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<Theme> Themes { get; }

    DbSet<LoginHistory> LoginHistories { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>Persists pending changes, running audit/soft-delete/tenant logic.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
