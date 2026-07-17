using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Domain.Enums;
using CMS.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Observability;

// ---- System logs ---------------------------------------------------------

public sealed record SystemLogDto(Guid Id, string Level, string Message, string? Exception, string? SourceContext, string? RequestPath, DateTimeOffset TimestampUtc);

public sealed record GetSystemLogsQuery(string? Level, string? Search, int Page = 1, int PageSize = 25) : IRequest<PagedResult<SystemLogDto>>;

public sealed class GetSystemLogsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSystemLogsQuery, PagedResult<SystemLogDto>>
{
    public async Task<PagedResult<SystemLogDto>> Handle(GetSystemLogsQuery request, CancellationToken cancellationToken)
    {
        var query = db.SystemLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            query = query.Where(l => l.Level == request.Level);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(l => l.Message.Contains(term));
        }

        return await query
            .OrderByDescending(l => l.TimestampUtc)
            .Select(l => new SystemLogDto(l.Id, l.Level, l.Message, l.Exception, l.SourceContext, l.RequestPath, l.TimestampUtc))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

// ---- Job runs ------------------------------------------------------------

public sealed record JobRunDto(Guid Id, string JobName, string JobType, JobStatus Status, int Progress, DateTimeOffset? StartedOnUtc, DateTimeOffset? FinishedOnUtc, string? Error, string? Result, string? TriggeredBy);

public sealed record GetJobRunsQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<JobRunDto>>;

public sealed class GetJobRunsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetJobRunsQuery, PagedResult<JobRunDto>>
{
    public async Task<PagedResult<JobRunDto>> Handle(GetJobRunsQuery request, CancellationToken cancellationToken) =>
        await db.JobRuns.AsNoTracking()
            .OrderByDescending(j => j.StartedOnUtc)
            .Select(j => new JobRunDto(j.Id, j.JobName, j.JobType, j.Status, j.Progress, j.StartedOnUtc, j.FinishedOnUtc, j.Error, j.Result, j.TriggeredBy))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
}
