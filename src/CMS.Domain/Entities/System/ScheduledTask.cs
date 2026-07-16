using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.System;

/// <summary>
/// A user-configurable recurring task definition. The scheduler reads enabled
/// tasks and registers them (by <see cref="CronExpression"/>) with the background
/// job engine. Tracks last/next run for visibility.
/// </summary>
public class ScheduledTask : AuditableEntity
{
    /// <summary>Unique task name.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Cron expression controlling the schedule, e.g. "0 2 * * *".</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>Identifier of the job implementation to run.</summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>Optional JSON parameters passed to the job.</summary>
    public string? Parameters { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset? LastRunOnUtc { get; set; }
    public DateTimeOffset? NextRunOnUtc { get; set; }
    public JobStatus? LastStatus { get; set; }
}
