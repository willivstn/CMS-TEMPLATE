using System.Text;
using System.Text.Json;
using CMS.Application.Features.Theming;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.ViewComponents;

/// <summary>
/// Injects the active custom theme's CSS custom-property overrides as a
/// <c>&lt;style&gt;</c> block. The theme is chosen via the "cms-theme-key" cookie;
/// its <c>Variables</c> (a JSON map of <c>--bs-*</c> overrides) are rendered as
/// <c>:root</c> rules layered over Bootstrap's light/dark mode.
/// </summary>
public sealed class ThemeStyleViewComponent(ISender mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var key = Request.Cookies["cms-theme-key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            return View((object)string.Empty);
        }

        var theme = await mediator.Send(new GetThemeByKeyQuery(key));
        return View((object)BuildCss(theme?.Variables));
    }

    private static string BuildCss(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
        {
            return string.Empty;
        }

        try
        {
            var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(variablesJson);
            if (variables is null || variables.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(":root{");
            foreach (var (name, value) in variables)
            {
                // Only emit well-formed custom properties with safe values.
                if (name.StartsWith("--", StringComparison.Ordinal) && IsSafe(name) && IsSafe(value))
                {
                    builder.Append(name).Append(':').Append(value).Append(';');
                }
            }

            builder.Append('}');
            return builder.ToString();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static bool IsSafe(string value) => !value.Contains('<') && !value.Contains('}') && !value.Contains(';');
}
