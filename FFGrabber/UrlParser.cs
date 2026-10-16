using System;
using System.Collections.Generic;

namespace FFGrabber;

public sealed class LinkInfo
{
    public string Id { get; }
    public string OriginalUrl { get; }
    public string Filename { get; }

    public LinkInfo(string id, string originalUrl, string filename)
    {
        Id = id;
        OriginalUrl = originalUrl;
        Filename = filename;
    }
}

public static class UrlParser
{
    private static readonly string[] KnownHosts = {
        "fuckingfast.co",
        "fuckingfast.com",
    };

    public static string? ParseFileId(string url)
    {
        url = url.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        bool hostMatch = false;
        foreach (var host in KnownHosts)
        {
            if (uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase))
            {
                hostMatch = true;
                break;
            }
        }
        if (!hostMatch)
            return null;

        // path is /{id} or /{id}/ — strip slashes
        string path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(path))
            return null;

        // reject paths with multiple segments (e.g. /f/id/go)
        if (path.Contains('/'))
            return null;

        return path;
    }

    public static string ExtractFilename(string url)
    {
        int hashIdx = url.IndexOf('#');
        if (hashIdx < 0 || hashIdx == url.Length - 1)
            return "(unknown)";

        return Uri.UnescapeDataString(url.Substring(hashIdx + 1));
    }

    public static List<LinkInfo> ParseInput(string rawInput)
    {
        var results = new List<LinkInfo>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] lines = rawInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string? id = ParseFileId(line);
            if (id == null)
                continue;

            if (!seenIds.Add(id))
                continue; // duplicate

            string filename = ExtractFilename(line);
            results.Add(new LinkInfo(id, line, filename));
        }

        return results;
    }
}
