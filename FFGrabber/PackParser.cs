using System.Text.RegularExpressions;

namespace FFGrabber;

public static class PackParser
{
    private static readonly Regex PartSuffix = new(
        @"^(.+?)[\._\-]+part\d+\.rar$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string IdentifyPack(string filename)
    {
        var m = PartSuffix.Match(filename);
        if (m.Success)
            return m.Groups[1].Value;

        int dot = filename.LastIndexOf('.');
        return dot > 0 ? filename[..dot] : filename;
    }

    public static List<DownloadItem> CreateItems(List<LinkInfo> links)
    {
        var items = new List<DownloadItem>(links.Count);
        foreach (var link in links)
            items.Add(new DownloadItem(link, IdentifyPack(link.Filename), link.Filename));

        items.Sort((a, b) =>
        {
            int c = string.Compare(a.PackName, b.PackName, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Filename, b.Filename, StringComparison.OrdinalIgnoreCase);
        });

        return items;
    }
}
