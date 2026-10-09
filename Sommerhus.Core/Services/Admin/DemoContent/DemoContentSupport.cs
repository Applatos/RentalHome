using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;

namespace Sommerhus.Core.Services.Admin.DemoContent;

/// <summary>
/// Stops an import; its message becomes the report's error.
/// </summary>
public sealed class DemoContentException(string message) : Exception(message);

/// <summary>
/// What one import run did: changes it made and things it deliberately left alone.
/// </summary>
public sealed class DemoContentLog
{
    public List<string> Changes { get; } = new();

    public List<string> Notes { get; } = new();

    /// <param name="parts">What was added, such as "4 images".</param>
    public void Report(string kind, string name, bool created, IReadOnlyList<string> parts, bool published)
    {
        var added = parts.Count > 0 ? string.Join(", ", parts) : null;
        var what = (created, added) switch
        {
            (true, null) => "created",
            (true, _) => $"created with {added}",
            (false, null) => null,
            (false, _) => $"added {added}",
        };

        if (published)
            what = what is null ? "published" : $"{what}, published";
        if (what is not null)
            Changes.Add($"{kind} '{name}': {what}");
    }
}

internal static class DemoContentResults
{
    /// <summary>
    /// The result's value, or a <see cref="DemoContentException"/> naming what failed and why.
    /// </summary>
    public static T OrThrow<T>(this ServiceResult<T> result, string what)
    {
        ((ServiceResult)result).OrThrow(what);
        return result.Value!;
    }

    public static void OrThrow(this ServiceResult result, string what)
    {
        if (result.IsSuccess)
            return;

        var details = result.Errors.SelectMany(e => e.Value.Select(m => string.IsNullOrEmpty(e.Key) ? m : $"{e.Key}: {m}"));
        throw new DemoContentException($"{what} failed ({result.Status}). {string.Join(" ", details)}".TrimEnd());
    }
}

/// <summary>
/// Content files opened as uploads, which is what the admin services accept.
/// </summary>
internal sealed class DemoContentUploads : IDisposable
{
    private readonly List<Stream> streams = new();

    public DemoContentUploads(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path))
                throw new DemoContentException($"File not found: {path}");

            var stream = File.OpenRead(path);
            streams.Add(stream);
            Files.Add(new FormFile(stream, 0, stream.Length, "files", Path.GetFileName(path))
            {
                Headers = new HeaderDictionary(),
                ContentType = DemoContentFiles.ContentTypeFor(path)
            });
        }
    }

    public FormFileCollection Files { get; } = new();

    public void Dispose() => streams.ForEach(s => s.Dispose());
}
