using CMS.Shared.Text;

namespace CMS.UnitTests;

public class SlugTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Café del Mar  ", "cafe-del-mar")]
    [InlineData("A/B & C", "a-b-c")]
    [InlineData("Multiple   spaces", "multiple-spaces")]
    [InlineData("Trailing---dashes--", "trailing-dashes")]
    public void Generate_produces_expected_slug(string input, string expected)
        => Assert.Equal(expected, Slug.Generate(input));

    [Fact]
    public void Generate_returns_empty_for_whitespace()
        => Assert.Equal(string.Empty, Slug.Generate("   "));

    [Fact]
    public void Generate_respects_max_length()
    {
        var slug = Slug.Generate(new string('a', 300), maxLength: 50);
        Assert.True(slug.Length <= 50);
    }
}
