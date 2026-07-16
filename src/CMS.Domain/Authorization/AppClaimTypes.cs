namespace CMS.Domain.Authorization;

/// <summary>Custom claim types issued into the authenticated principal.</summary>
public static class AppClaimTypes
{
    /// <summary>A single effective permission the user holds (one claim per permission).</summary>
    public const string Permission = "permission";

    /// <summary>The tenant the user's session is scoped to.</summary>
    public const string TenantId = "tenant_id";
}
