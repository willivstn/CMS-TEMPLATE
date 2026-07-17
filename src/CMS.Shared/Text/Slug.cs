using System.Globalization;
using System.Text;

namespace CMS.Shared.Text;

/// <summary>Generates URL-friendly slugs from arbitrary text.</summary>
public static class Slug
{
    /// <summary>
    /// Produces a lowercase, hyphen-separated slug: strips diacritics, drops
    /// non-alphanumeric characters, and collapses whitespace/dashes.
    /// </summary>
    public static string Generate(string input, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue; // drop accents
            }

            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length > maxLength ? slug[..maxLength].Trim('-') : slug;
    }
}
