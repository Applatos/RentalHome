using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Html;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class TextParagraphsTests
{
    [Theory]
    [InlineData("Første afsnit.\nAnden linje.\n\nAndet afsnit.", "<p>Første afsnit.<br>Anden linje.</p><p>Andet afsnit.</p>")]
    [InlineData("Windows\r\nlinjer\r\n\r\nnyt afsnit", "<p>Windows<br>linjer</p><p>nyt afsnit</p>")]
    [InlineData("  \n\nKun et afsnit  \n\n\n", "<p>Kun et afsnit</p>")]
    [InlineData("<script>alert(1)</script> & mere", "<p>&lt;script&gt;alert(1)&lt;/script&gt; &amp; mere</p>")]
    public void Render_SplitsParagraphs_AndEncodesText(string text, string expected)
        => Html(TextParagraphs.Render(text)).Should().Be(expected);

    [Fact]
    public void Render_EmptyText_RendersNothing()
        => Html(TextParagraphs.Render("   ")).Should().BeEmpty();

    [Fact]
    public void Render_AddsTheParagraphClass()
        => Html(TextParagraphs.Render("Tekst", "lead")).Should().Be("<p class=\"lead\">Tekst</p>");

    // Encodes only what HTML needs, as the site's encoder is configured to.
    private static string Html(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
        return writer.ToString();
    }
}
