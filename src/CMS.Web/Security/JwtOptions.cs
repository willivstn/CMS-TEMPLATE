namespace CMS.Web.Security;

/// <summary>Binds the "Jwt" configuration section used to sign and validate API tokens.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "cms-template";
    public string Audience { get; set; } = "cms-template";

    /// <summary>HMAC signing key. Must be at least 32 characters. Override in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
