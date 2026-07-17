using CMS.Application.Common.Abstractions;
using CMS.Domain.Enums;
using CMS.Web.Email;
using Microsoft.EntityFrameworkCore;

namespace CMS.Web.Jobs;

/// <summary>Publishes content whose scheduled time has arrived. Runs on a schedule via Hangfire.</summary>
public sealed class ScheduledContentPublisher(IApplicationDbContext db, IDateTimeProvider clock)
{
    public Task PublishDueAsync() => JobTracking.RunAsync(db, clock, "Scheduled content publisher", async () =>
    {
        var now = clock.UtcNow;
        var due = await db.ContentItems
            .Where(c => c.Status == ContentStatus.Scheduled && c.ScheduledOnUtc != null && c.ScheduledOnUtc <= now)
            .ToListAsync();

        foreach (var item in due)
        {
            item.Status = ContentStatus.Published;
            item.PublishedOnUtc = now;
            item.ScheduledOnUtc = null;
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        return $"Published {due.Count} scheduled item(s).";
    });
}

/// <summary>Drains the outbound email queue, sending via SMTP with retry/backoff.</summary>
public sealed class EmailQueueProcessor(IApplicationDbContext db, IDateTimeProvider clock, SmtpEmailDispatcher dispatcher)
{
    public Task ProcessAsync() => JobTracking.RunAsync(db, clock, "Email queue", async () =>
    {
        var now = clock.UtcNow;
        var batch = await db.EmailMessages
            .Where(m => m.Status == EmailStatus.Queued
                && (m.ScheduledOnUtc == null || m.ScheduledOnUtc <= now)
                && m.Attempts < m.MaxAttempts)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(25)
            .ToListAsync();

        var sent = 0;
        foreach (var message in batch)
        {
            try
            {
                await dispatcher.SendAsync(message);
                message.Status = EmailStatus.Sent;
                message.SentOnUtc = now;
                sent++;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                if (message.Attempts >= message.MaxAttempts)
                {
                    message.Status = EmailStatus.Failed;
                }
            }
        }

        if (batch.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        return $"Sent {sent} of {batch.Count} queued email(s).";
    });
}

/// <summary>Prunes old system logs and audit entries to keep the tables lean.</summary>
public sealed class CleanupJob(IApplicationDbContext db, IDateTimeProvider clock)
{
    public Task RunAsync() => JobTracking.RunAsync(db, clock, "Cleanup", async () =>
    {
        var logCutoff = clock.UtcNow.AddDays(-30);
        var auditCutoff = clock.UtcNow.AddDays(-90);

        var logs = await db.SystemLogs.Where(l => l.TimestampUtc < logCutoff).ExecuteDeleteAsync();
        var audits = await db.AuditLogs.Where(a => a.TimestampUtc < auditCutoff).ExecuteDeleteAsync();

        return $"Removed {logs} log(s) and {audits} audit row(s).";
    });
}
