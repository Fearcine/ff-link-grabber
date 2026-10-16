using Microsoft.Web.WebView2.Core;

namespace FFGrabber;

public enum DownloadStatus
{
    Queued,
    Extracting,
    Downloading,
    Paused,
    Done,
    Failed,
    Skipped
}

public class DownloadItem
{
    public LinkInfo Link { get; }
    public string PackName { get; }
    public string Filename { get; }
    public bool Selected { get; set; } = true;
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public long PrevBytes { get; set; }
    public DateTime PrevCheck { get; set; } = DateTime.UtcNow;
    public double SpeedBps { get; set; }
    public string? Error { get; set; }
    public CoreWebView2DownloadOperation? Operation { get; set; }

    public DownloadItem(LinkInfo link, string packName, string filename)
    {
        Link = link;
        PackName = packName;
        Filename = filename;
    }
}
