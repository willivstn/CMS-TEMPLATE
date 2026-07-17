using CMS.Application.Common.Abstractions;
using CMS.Application.Features.Observability;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using CMS.Web.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Background-job history and manual triggers (Hangfire dashboard at /hangfire).</summary>
[Area("Admin")]
[HasPermission(Permissions.System.ManageJobs)]
public sealed class JobsController(ISender mediator, IBackgroundJobService jobs) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
        => View(await mediator.Send(new GetJobRunsQuery(page), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Run(string job)
    {
        switch (job)
        {
            case "content":
                jobs.Enqueue<ScheduledContentPublisher>(j => j.PublishDueAsync());
                break;
            case "email":
                jobs.Enqueue<EmailQueueProcessor>(j => j.ProcessAsync());
                break;
            case "cleanup":
                jobs.Enqueue<CleanupJob>(j => j.RunAsync());
                break;
        }

        TempData["flash:success"] = "Job queued.";
        return RedirectToAction(nameof(Index));
    }
}
