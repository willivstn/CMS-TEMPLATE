using System.Net;
using System.Net.Http.Json;
using CMS.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CMS.IntegrationTests;

/// <summary>
/// Boots the whole application in-process (WebApplicationFactory) and exercises the
/// REST API over HTTP. Requires a reachable SQL Server (the app migrates + seeds on
/// startup), so these run locally / with a database — not in the DB-less CI job.
/// </summary>
public class ApiIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Public_content_endpoint_returns_ok()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/content");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_authentication()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/content/whoami");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Security_headers_are_present()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/content");
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Login_with_seeded_admin_issues_a_token()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@cms.local", "Admin#12345"));

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
    }
}
