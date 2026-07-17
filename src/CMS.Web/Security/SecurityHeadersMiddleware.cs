namespace CMS.Web.Security;

/// <summary>
/// Adds baseline security response headers and a Content-Security-Policy. The CSP
/// permits inline scripts/styles because the template (and Swagger/Hangfire UIs)
/// use them; tightening to nonce-based CSP by removing inline handlers is the
/// recommended hardening step for a specific deployment.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string Csp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'self'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Content-Security-Policy"] = Csp;
        headers.Remove("X-Powered-By");

        await next(context);
    }
}
