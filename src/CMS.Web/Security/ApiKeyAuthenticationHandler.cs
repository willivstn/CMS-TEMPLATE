using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CMS.Web.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// Authenticates requests carrying an <c>X-Api-Key</c> header. The key's prefix
/// locates the stored record; the full key is compared by hash in constant time.
/// The resulting principal carries the key's scopes as permission claims, so the
/// same <c>[HasPermission]</c> policies apply to API-key callers.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var header) || string.IsNullOrWhiteSpace(header))
        {
            return AuthenticateResult.NoResult();
        }

        var provided = header.ToString();
        var dot = provided.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0)
        {
            return AuthenticateResult.Fail("Malformed API key.");
        }

        var prefix = provided[..dot];
        var db = Context.RequestServices.GetRequiredService<IApplicationDbContext>();
        var clock = Context.RequestServices.GetRequiredService<IDateTimeProvider>();

        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Prefix == prefix);
        if (key is null || !key.IsActive || (key.ExpiresOnUtc is { } exp && exp <= clock.UtcNow))
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var providedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(provided)));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash), Encoding.UTF8.GetBytes(key.KeyHash)))
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        key.LastUsedUtc = clock.UtcNow;
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, key.Name),
            new(ClaimTypes.NameIdentifier, key.Id.ToString()),
        };

        foreach (var scope in (key.Scopes ?? string.Empty)
                     .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(AppClaimTypes.Permission, scope));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
