using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Sommerhus.Mvc.Controllers;

public class AreasController(IWebHostEnvironment env) : Controller
{
    public record AreaJson(string Slug, string Title, List<string> Images, string? Intro);
    public record AreaPageVm(string Slug, string Title, string Html, List<string> Images);

    [HttpGet("/area/{slug}")]
    public IActionResult Details(string slug)
    {
        var jsonPath = Path.Combine(env.WebRootPath, "areas", $"{slug}.json");
        if (!System.IO.File.Exists(jsonPath)) return NotFound();

        // Læs JSON (UTF-8 forventes)
        var json = System.IO.File.ReadAllText(jsonPath, Encoding.UTF8);
        var meta = JsonSerializer.Deserialize<AreaJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (meta is null) return NotFound();

        // Læs Markdown hvis tilgængelig (UTF-8) – ellers brug Intro fra JSON
        var mdPath = Path.Combine(env.WebRootPath, "areas", $"{slug}.md");
        string html;
        if (System.IO.File.Exists(mdPath))
        {
            var md = System.IO.File.ReadAllText(mdPath, Encoding.UTF8);
            html = ToHtmlParagraphs(md);
        }
        else
        {
            html = ToHtmlParagraphs(meta.Intro ?? "");
        }

        var vm = new AreaPageVm(slug, meta.Title, html, meta.Images ?? new());
        return View(vm);
    }

    private static string ToHtmlParagraphs(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "<p></p>";
        // enkelt, robust: split på tomme linjer -> <p>, øvrige linjeskift -> <br/>
        var parts = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            var lines = p.Split('\n');
            var para = string.Join("<br/>", lines.Select(l => System.Net.WebUtility.HtmlEncode(l)));
            sb.Append("<p>").Append(para).Append("</p>");
        }
        return sb.ToString();
    }
}
