using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.System;
using CMS.Domain.Enums;

namespace CMS.Web.Jobs;

/// <summary>
/// Wraps a background job so each execution is recorded as a <see cref="JobRun"/>
/// (for the admin Jobs page) with status, timing and error captured.
/// </summary>
public static class JobTracking
{
    public static async Task RunAsync(
        IApplicationDbContext db, IDateTimeProvider clock, string jobName, Func<Task<string?>> work)
    {
        var run = new JobRun
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            JobType = "Recurring",
            Status = JobStatus.Processing,
            StartedOnUtc = clock.UtcNow,
            TriggeredBy = "system",
        };

        db.JobRuns.Add(run);
        await db.SaveChangesAsync();

        try
        {
            run.Result = await work();
            run.Status = JobStatus.Succeeded;
        }
        catch (Exception ex)
        {
            run.Status = JobStatus.Failed;
            run.Error = ex.Message;
        }
        finally
        {
            run.Progress = 100;
            run.FinishedOnUtc = clock.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
