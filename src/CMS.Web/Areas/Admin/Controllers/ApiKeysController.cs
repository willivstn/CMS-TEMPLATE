using CMS.Application.Features.ApiKeys;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Manages REST API keys. The plaintext key is shown once on creation.</summary>
[Area("Admin")]
[HasPermission(Permissions.System.ManageApiKeys)]
public sealed class ApiKeysController(ISender mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetApiKeysQuery(), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? scopes, DateTime? expiresOn, CancellationToken ct)
    {
        DateTimeOffset? expiry = expiresOn.HasValue ? new DateTimeOffset(expiresOn.Value.Date, TimeSpan.Zero) : null;
        var result = await mediator.Send(new CreateApiKeyCommand(name ?? string.Empty, scopes, expiry), ct);

        if (result.IsSuccess)
        {
            TempData["ApiKey:Plain"] = result.Value.PlainKey;
            TempData["flash:success"] = "API key created. Copy it now — it will not be shown again.";
        }
        else
        {
            TempData["flash:error"] = result.Error.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await mediator.Send(new RevokeApiKeyCommand(id), ct);
        TempData["flash:success"] = "API key revoked.";
        return RedirectToAction(nameof(Index));
    }
}
