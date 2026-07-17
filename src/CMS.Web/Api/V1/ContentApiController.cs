using Asp.Versioning;
using CMS.Application.Features.Content;
using CMS.Contracts;
using CMS.Domain.Authorization;
using CMS.Domain.Enums;
using CMS.Web.Authorization;
using CMS.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CMS.Web.Api.V1;

/// <summary>
/// Public read API for published content, plus a token-protected endpoint that
/// demonstrates JWT/API-key authentication with permission-based authorization.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/content")]
[Produces("application/json")]
[EnableRateLimiting("api")]
public sealed class ContentApiController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IEnumerable<ContentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? type, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        ContentType? contentType = Enum.TryParse<ContentType>(type, ignoreCase: true, out var parsed) ? parsed : null;
        var result = await mediator.Send(new GetPublishedContentQuery(contentType, page), ct);
        return Ok(result.Items.Select(Map));
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<ContentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var item = await mediator.Send(new GetPublishedContentBySlugQuery(slug), ct);
        return item is null ? NotFound() : Ok(Map(item));
    }

    /// <summary>Returns the authenticated caller's identity — requires a JWT or API key with Content.View.</summary>
    [HttpGet("whoami")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthenticationHandler.SchemeName}")]
    [HasPermission(Permissions.Content.View)]
    public IActionResult WhoAmI() => Ok(new
    {
        name = User.Identity?.Name,
        authenticationType = User.Identity?.AuthenticationType,
        permissions = User.FindAll(AppClaimTypes.Permission).Select(c => c.Value),
    });

    private static ContentResponse Map(PublicContentDto d) =>
        new(d.Id, d.Type.ToString(), d.Title, d.Slug, d.Summary, d.Body, d.Category, d.Author, d.Tags, d.PublishedOnUtc);
}
