using Microsoft.Playwright;

namespace CMS.UiTests;

/// <summary>
/// End-to-end browser tests (Playwright). Skipped by default because they need a
/// running server and installed browsers. To enable:
///   1. dotnet build
///   2. pwsh tests/CMS.UiTests/bin/Debug/net10.0/playwright.ps1 install
///   3. run the app (dotnet run --project src/CMS.Web) and set CMS_BASE_URL
///   4. remove the Skip below and run: dotnet test tests/CMS.UiTests
/// </summary>
public class LoginFlowTests
{
    private static string BaseUrl => Environment.GetEnvironmentVariable("CMS_BASE_URL") ?? "http://localhost:5200";

    [Fact(Skip = "E2E: requires a running server and 'playwright install'. See class remarks.")]
    public async Task Admin_can_sign_in_and_reach_the_dashboard()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Account/Login");
        await page.GetByLabel("Email").FillAsync("admin@cms.local");
        await page.GetByLabel("Password").FillAsync("Admin#12345");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in" }).ClickAsync();

        await page.GotoAsync($"{BaseUrl}/Admin/Dashboard");
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Dashboard");
    }
}
