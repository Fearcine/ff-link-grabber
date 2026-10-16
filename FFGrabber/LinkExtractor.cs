using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace FFGrabber;

public sealed class LinkExtractor : IDisposable
{
    private readonly WebView2 _webView;
    private readonly string _userDataFolder;
    private bool _initialized;

    private const int TurnstileTimeoutMs = 60000;
    private const int TurnstilePollMs = 400;
    private const int PageLoadTimeoutMs = 30000;
    private const int DownloadStartTimeoutMs = 20000;

    public LinkExtractor(WebView2 webView)
    {
        _webView = webView;
        _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FFGrabber", "WebView2Data");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        Directory.CreateDirectory(_userDataFolder);
        var env = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
        await _webView.EnsureCoreWebView2Async(env);

        // block ad popups
        _webView.CoreWebView2.NewWindowRequested += (s, e) => { e.Handled = true; };

        _initialized = true;
    }

    /// <summary>
    /// Navigates to the fuckingfast link, waits for Turnstile, clicks download,
    /// and returns the DownloadOperation. The file saves to saveFolder.
    /// </summary>
    public async Task<CoreWebView2DownloadOperation?> StartDownload(
        LinkInfo link, string saveFolder, CancellationToken ct)
    {
        // navigate to fuckingfast page
        await NavigateAndWait(link.OriginalUrl, ct);
        await Task.Delay(1500, ct);

        // wait for turnstile
        bool ready = await WaitForTurnstileReady(ct);
        if (!ready)
            throw new Exception("turnstile did not solve within timeout");

        // wire up download capture
        var downloadTcs = new TaskCompletionSource<CoreWebView2DownloadOperation>();

        void OnDownloadStarting(object? s, CoreWebView2DownloadStartingEventArgs e)
        {
            // use server-suggested filename, fall back to URL hash
            string fname = Path.GetFileName(e.ResultFilePath);
            if (string.IsNullOrEmpty(fname))
                fname = SanitizeFilename(link.Filename);

            e.ResultFilePath = Path.Combine(saveFolder, fname);
            e.Handled = true; // suppress save dialog
            downloadTcs.TrySetResult(e.DownloadOperation);
        }

        _webView.CoreWebView2.DownloadStarting += OnDownloadStarting;

        try
        {
            // click download button (twice — site opens ad on first click, download on second)
            await ClickDownloadButton();
            await Task.Delay(700, ct);
            await ClickDownloadButton();

            // wait for download to actually start
            var op = await WaitWithTimeout(downloadTcs.Task, DownloadStartTimeoutMs, ct);
            return op;
        }
        finally
        {
            _webView.CoreWebView2.DownloadStarting -= OnDownloadStarting;
        }
    }

    private async Task NavigateAndWait(string url, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();

        void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e)
        {
            tcs.TrySetResult(e.IsSuccess);
        }

        _webView.CoreWebView2.NavigationCompleted += Handler;
        try
        {
            _webView.CoreWebView2.Navigate(url);
            bool ok = await WaitWithTimeout(tcs.Task, PageLoadTimeoutMs, ct);
            if (!ok)
                throw new Exception("page load failed");
        }
        finally
        {
            _webView.CoreWebView2.NavigationCompleted -= Handler;
        }
    }

    private async Task<bool> WaitForTurnstileReady(CancellationToken ct)
    {
        int elapsed = 0;
        while (elapsed < TurnstileTimeoutMs)
        {
            ct.ThrowIfCancellationRequested();

            string js = @"
                (function() {
                    if (window.turnstileToken) return 'token';
                    if (window.diCleared) return 'cleared';
                    var btn = document.querySelector('a[hx-post]');
                    if (btn) {
                        var cs = window.getComputedStyle(btn);
                        if (cs.opacity === '1') return 'button_ready';
                    }
                    return '';
                })()
            ";

            string result = await _webView.CoreWebView2.ExecuteScriptAsync(js);
            string cleaned = result.Trim('"');

            if (cleaned == "token" || cleaned == "cleared" || cleaned == "button_ready")
                return true;

            await Task.Delay(TurnstilePollMs, ct);
            elapsed += TurnstilePollMs;
        }

        return false;
    }

    private async Task ClickDownloadButton()
    {
        await _webView.CoreWebView2.ExecuteScriptAsync(@"
            (function() {
                var btn = document.querySelector('a[hx-post]');
                if (btn) btn.click();
            })()
        ");
    }

    private static string SanitizeFilename(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, int timeoutMs, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var delay = Task.Delay(timeoutMs, cts.Token);
        var winner = await Task.WhenAny(task, delay);
        if (winner == task)
        {
            cts.Cancel();
            return await task;
        }
        ct.ThrowIfCancellationRequested();
        throw new TimeoutException("operation timed out");
    }

    public void Dispose() { }
}
