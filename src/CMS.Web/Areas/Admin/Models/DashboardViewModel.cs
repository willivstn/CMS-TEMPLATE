using CMS.Domain.Enums;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Data for the admin dashboard landing page.</summary>
public sealed class DashboardViewModel
{
    public int UserCount { get; init; }
    public int RoleCount { get; init; }
    public int ContentCount { get; init; }
    public int MediaCount { get; init; }

    public IReadOnlyList<ActivityRow> RecentActivity { get; init; } = [];
    public IReadOnlyList<RecentUserRow> RecentUsers { get; init; } = [];
}

/// <summary>A recent audit-trail entry shown on the dashboard.</summary>
public sealed record ActivityRow(AuditAction Action, string? Entity, string? User, DateTimeOffset When);

/// <summary>A recently-registered user shown on the dashboard.</summary>
public sealed record RecentUserRow(string Name, string? Email, DateTimeOffset When);
