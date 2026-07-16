using System.Text.Json;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Common;
using CMS.Domain.Entities.System;
using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CMS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// A SaveChanges interceptor that, in one pass over the change tracker:
/// <list type="bullet">
///   <item>stamps <see cref="IAuditable"/> created/modified fields,</item>
///   <item>stamps <see cref="ITenantEntity.TenantId"/> on new rows,</item>
///   <item>converts hard deletes of <see cref="ISoftDeletable"/> entities into
///         soft deletes, and</item>
///   <item>captures before/after snapshots into <see cref="AuditLog"/> rows,
///         written in the same transaction.</item>
/// </list>
/// Guid keys are generated client-side by EF, so entity ids are available for the
/// audit trail even for freshly-added rows.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ITenantContext tenant) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Apply(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Apply(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext context)
    {
        var now = clock.UtcNow;
        var user = currentUser.UserName ?? "system";
        var auditLogs = new List<AuditLog>();

        // Snapshot the current entries first; we mutate the change tracker below.
        var entries = context.ChangeTracker.Entries().ToList();

        foreach (var entry in entries)
        {
            StampAudit(entry, now, user);
            StampTenant(entry);
            ConvertSoftDelete(entry, now, user);

            if (ShouldAudit(entry))
            {
                auditLogs.Add(BuildAuditLog(entry, now));
            }
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static void StampAudit(EntityEntry entry, DateTimeOffset now, string user)
    {
        if (entry.Entity is not IAuditable auditable)
        {
            return;
        }

        if (entry.State == EntityState.Added)
        {
            auditable.CreatedOnUtc = now;
            auditable.CreatedBy = user;
        }
        else if (entry.State == EntityState.Modified)
        {
            auditable.ModifiedOnUtc = now;
            auditable.ModifiedBy = user;
        }
    }

    private void StampTenant(EntityEntry entry)
    {
        if (entry is { State: EntityState.Added, Entity: ITenantEntity tenantEntity }
            && tenantEntity.TenantId == Guid.Empty)
        {
            tenantEntity.TenantId = tenant.CurrentTenantId;
        }
    }

    private static void ConvertSoftDelete(EntityEntry entry, DateTimeOffset now, string user)
    {
        if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deletable })
        {
            entry.State = EntityState.Modified;
            deletable.IsDeleted = true;
            deletable.DeletedOnUtc = now;
            deletable.DeletedBy = user;
        }
    }

    /// <summary>Audit entities that opt in via <see cref="IAuditable"/>, but never the trail itself.</summary>
    private static bool ShouldAudit(EntityEntry entry) =>
        entry.Entity is IAuditable
        && entry.Entity is not AuditLog
        && entry.Entity is not SystemLog
        && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private AuditLog BuildAuditLog(EntityEntry entry, DateTimeOffset now)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changedColumns = new List<string>();

        // A soft delete has already been rewritten to Modified; detect it.
        var isSoftDelete = entry.Entity is ISoftDeletable { IsDeleted: true }
            && entry.State == EntityState.Modified
            && entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified;

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    oldValues[name] = property.OriginalValue;
                    break;

                case EntityState.Modified when property.IsModified:
                    oldValues[name] = property.OriginalValue;
                    newValues[name] = property.CurrentValue;
                    changedColumns.Add(name);
                    break;
            }
        }

        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            _ => isSoftDelete ? AuditAction.Delete : AuditAction.Update,
        };

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = (entry.Entity as ITenantEntity)?.TenantId ?? tenant.CurrentTenantId,
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            Action = action,
            EntityName = entry.Entity.GetType().Name,
            EntityId = TryGetPrimaryKey(entry),
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            AffectedColumns = changedColumns.Count > 0 ? string.Join(',', changedColumns) : null,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            TimestampUtc = now,
        };
    }

    private static string? TryGetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        var values = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => v is not null);

        return string.Join(",", values);
    }
}
