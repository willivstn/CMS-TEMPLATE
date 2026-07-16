using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.System;

/// <summary>
/// A record of a single background-job execution, providing history and live
/// progress (surfaced on the dashboard over SignalR). Complements Hangfire's own
/// storage with domain-friendly, queryable metadata.
/// </summary>
public class JobRun : Entity<Guid>
{
    public string JobName { get; set; } = string.Empty;

    /// <summary>The job kind, e.g. "Recurring", "Scheduled", "FireAndForget".</summary>
    public string JobType { get; set; } = string.Empty;

    public JobStatus Status { get; set; } = JobStatus.Queued;

    public DateTimeOffset? StartedOnUtc { get; set; }
    public DateTimeOffset? FinishedOnUtc { get; set; }

    /// <summary>Completion percentage (0–100) for long-running jobs.</summary>
    public int Progress { get; set; }

    public string? Error { get; set; }

    /// <summary>Optional serialized result payload.</summary>
    public string? Result { get; set; }

    /// <summary>Who or what triggered the run (user id or "system").</summary>
    public string? TriggeredBy { get; set; }
}
