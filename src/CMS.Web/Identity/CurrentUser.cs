using System.Security.Claims;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Authorization;

namespace CMS.Web.Identity;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the request's <see cref="HttpContext"/>.
/// Reads identity and permissions from the authenticated principal's claims, so
/// permission checks require no database access. Falls back to the system identity
/// outside a request (e.g. during startup seeding or background work).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.Identity?.IsAuthenticated == true
        ? Principal.FindFirstValue(ClaimTypes.Name) ?? Principal.FindFirstValue(ClaimTypes.Email)
        : "system";

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(AppClaimTypes.Permission, permission) ?? false;
}
