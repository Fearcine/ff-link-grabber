using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace FFGrabber;

public partial class MainForm : Form
{
    private List<DownloadItem> _items = new();
    private LinkExtractor? _extractor;
    private CancellationTokenSource? _cts;
    private System.Windows.Forms.Timer _refreshTimer;
    private bool _isRunning;

    public MainForm()
    {
        InitializeComponent();
        WireEvents();

        _refreshTimer = new System.Windows.Forms.Timer();
        _refreshTimer.Interval = 500;
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void WireEvents()
    {
        btnParse.Click += OnParseClick;
        btnBrowse.Click += OnBrowseClick;
        btnStart.Click += OnStartClick;
        btnPauseAll.Click += OnPauseAllClick;
        btnResumeAll.Click += OnResumeAllClick;
        btnCancel.Click += OnCancelClick;

        ctxPause.Click += OnCtxPauseClick;
        ctxResume.Click += OnCtxResumeClick;
        ctxSkip.Click += OnCtxSkipClick;

        // commit checkbox changes immediately
        dgvDownloads.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvDownloads.IsCurrentCellDirty)
                dgvDownloads.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        dgvDownloads.CellValueChanged += (s, e) =>
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                var item = dgvDownloads.Rows[e.RowIndex].Tag as DownloadItem;
                if (item != null)
                    item.Selected = (bool)(dgvDownloads.Rows[e.RowIndex].Cells[0].Value ?? false);
            }
        };
    }

    // ── Parse ──

    private void OnParseClick(object? sender, EventArgs e)
    {
        string raw = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            MessageBox.Show("Paste some links first.", "No Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var links = UrlParser.ParseInput(raw);
        if (links.Count == 0)
        {
            MessageBox.Show("No valid fuckingfast.co URLs found.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _items = PackParser.CreateItems(links);
        PopulateGrid();
        Log($"parsed {_items.Count} files in {_items.Select(i => i.PackName).Distinct().Count()} pack(s)");
    }

    private void PopulateGrid()
    {
        dgvDownloads.Rows.Clear();
        foreach (var item in _items)
        {
            int idx = dgvDownloads.Rows.Add(
                item.Selected,
                item.PackName,
                item.Filename,
                "---",
                "---",
                item.Status.ToString());
            dgvDownloads.Rows[idx].Tag = item;
        }
    }

    // ── Browse ──

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        dlg.SelectedPath = txtSavePath.Text;
        dlg.Description = "Select download folder";
        if (dlg.ShowDialog() == DialogResult.OK)
            txtSavePath.Text = dlg.SelectedPath;
    }

    // ── Start Downloads ──

    private async void OnStartClick(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Parse links first.", "No Items",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string savePath = txtSavePath.Text.Trim();
        if (string.IsNullOrEmpty(savePath))
        {
            MessageBox.Show("Select a save folder.", "No Folder",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(savePath))
        {
            try { Directory.CreateDirectory(savePath); }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot create folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        SetRunningState(true);
        _cts = new CancellationTokenSource();
        _extractor = new LinkExtractor(webView);

        try
        {
            await _extractor.InitializeAsync();
            Log("webview2 ready");
            await RunDownloads(savePath, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log("--- extraction stopped ---");
        }
        catch (Exception ex)
        {
            Log($"error: {ex.Message}");
        }
        finally
        {
            _extractor?.Dispose();
            _extractor = null;
            _cts?.Dispose();
            _cts = null;
            SetRunningState(false);
        }
    }

    private async Task RunDownloads(string savePath, CancellationToken ct)
    {
        int maxParallel = (int)nudMaxParallel.Value;
        int delayMs = (int)nudDelay.Value;
        var sem = new SemaphoreSlim(maxParallel, maxParallel);
        var selected = _items.Where(i => i.Selected && i.Status == DownloadStatus.Queued).ToList();

        Log($"downloading {selected.Count} files (max {maxParallel} parallel)...");
        _refreshTimer.Start();

        try
        {
            foreach (var item in selected)
            {
                ct.ThrowIfCancellationRequested();

                // wait for a download slot
                await sem.WaitAsync(ct);

                item.Status = DownloadStatus.Extracting;
                RefreshItemRow(item);

                try
                {
                    var op = await _extractor!.StartDownload(item.Link, savePath, ct);
                    if (op == null)
                    {
                        item.Status = DownloadStatus.Failed;
                        item.Error = "download did not start";
                        Log($"FAIL {item.Filename}: download did not start");
                        sem.Release();
                        continue;
                    }

                    item.Operation = op;
                    item.TotalBytes = op.TotalBytesToReceive;
                    item.Status = DownloadStatus.Downloading;
                    item.PrevCheck = DateTime.UtcNow;
                    item.PrevBytes = 0;
                    Log($"downloading {item.Filename} ({FormatSize(item.TotalBytes)})");

                    // release semaphore when this download finishes
                    op.StateChanged += (s, args) =>
                    {
                        if (op.State == CoreWebView2DownloadState.Completed)
                        {
                            item.Status = DownloadStatus.Done;
                            Log($"done: {item.Filename}");
                        }
                        else if (op.State == CoreWebView2DownloadState.Interrupted)
                        {
                            if (item.Status != DownloadStatus.Paused)
                            {
                                item.Status = DownloadStatus.Failed;
                                Log($"failed: {item.Filename}");
                            }
                        }

                        if (op.State != CoreWebView2DownloadState.InProgress)
                        {
                            try { sem.Release(); } catch { }
                        }
                    };
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    item.Status = DownloadStatus.Failed;
                    item.Error = ex.Message;
                    Log($"FAIL {item.Filename}: {ex.Message}");
                    try { sem.Release(); } catch { }
                }

                // delay between link extractions
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }

            // wait for remaining active downloads to finish
            Log("all links extracted. waiting for active downloads...");
            while (_items.Any(i => i.Status == DownloadStatus.Downloading || i.Status == DownloadStatus.Extracting))
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(1000, ct);
            }

            int done = _items.Count(i => i.Status == DownloadStatus.Done);
            int failed = _items.Count(i => i.Status == DownloadStatus.Failed);
            Log($"--- finished. {done} done, {failed} failed ---");
        }
        finally
        {
            _refreshTimer.Stop();
            RefreshAllRows();
        }
    }

    // ── Pause / Resume / Cancel ──

    private void OnPauseAllClick(object? sender, EventArgs e)
    {
        foreach (var item in _items)
        {
            if (item.Status == DownloadStatus.Downloading && item.Operation != null)
            {
                item.Operation.Pause();
                item.Status = DownloadStatus.Paused;
            }
        }
        RefreshAllRows();
        Log("paused all");
    }

    private void OnResumeAllClick(object? sender, EventArgs e)
    {
        foreach (var item in _items)
        {
            if (item.Status == DownloadStatus.Paused && item.Operation != null)
            {
                item.Operation.Resume();
                item.Status = DownloadStatus.Downloading;
                item.PrevCheck = DateTime.UtcNow;
                item.PrevBytes = item.BytesReceived;
            }
        }
        RefreshAllRows();
        Log("resumed all");
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        btnCancel.Enabled = false;
        Log("cancelling extraction...");
    }

    // ── Context Menu ──

    private DownloadItem? GetSelectedItem()
    {
        if (dgvDownloads.SelectedRows.Count == 0) return null;
        return dgvDownloads.SelectedRows[0].Tag as DownloadItem;
    }

    private void OnCtxPauseClick(object? sender, EventArgs e)
    {
        var item = GetSelectedItem();
        if (item?.Status == DownloadStatus.Downloading && item.Operation != null)
        {
            item.Operation.Pause();
            item.Status = DownloadStatus.Paused;
            RefreshItemRow(item);
        }
    }

    private void OnCtxResumeClick(object? sender, EventArgs e)
    {
        var item = GetSelectedItem();
        if (item?.Status == DownloadStatus.Paused && item.Operation != null)
        {
            item.Operation.Resume();
            item.Status = DownloadStatus.Downloading;
            item.PrevCheck = DateTime.UtcNow;
            item.PrevBytes = item.BytesReceived;
            RefreshItemRow(item);
        }
    }

    private void OnCtxSkipClick(object? sender, EventArgs e)
    {
        var item = GetSelectedItem();
        if (item == null) return;

        if (item.Operation != null &&
            (item.Status == DownloadStatus.Downloading || item.Status == DownloadStatus.Paused))
        {
            item.Operation.Cancel();
        }

        item.Status = DownloadStatus.Skipped;
        item.Selected = false;
        RefreshItemRow(item);
    }

    // ── Grid Refresh ──

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dgvDownloads.Rows)
        {
            if (row.Tag is not DownloadItem item) continue;
            if (item.Operation == null) continue;
            if (item.Status != DownloadStatus.Downloading) continue;

            item.BytesReceived = item.Operation.BytesReceived;
            if (item.TotalBytes <= 0)
                item.TotalBytes = item.Operation.TotalBytesToReceive;

            var now = DateTime.UtcNow;
            double secs = (now - item.PrevCheck).TotalSeconds;
            if (secs >= 0.5)
            {
                long delta = item.BytesReceived - item.PrevBytes;
                item.SpeedBps = delta / secs;
                item.PrevBytes = item.BytesReceived;
                item.PrevCheck = now;
            }

            row.Cells["colProgress"].Value = FormatProgress(item);
            row.Cells["colSpeed"].Value = FormatSpeed(item);
            row.Cells["colStatus"].Value = item.Status.ToString();
        }
    }

    private void RefreshItemRow(DownloadItem item)
    {
        foreach (DataGridViewRow row in dgvDownloads.Rows)
        {
            if (row.Tag == item)
            {
                row.Cells["colSelect"].Value = item.Selected;
                row.Cells["colProgress"].Value = FormatProgress(item);
                row.Cells["colSpeed"].Value = FormatSpeed(item);
                row.Cells["colStatus"].Value = item.Status.ToString();
                break;
            }
        }
    }

    private void RefreshAllRows()
    {
        foreach (DataGridViewRow row in dgvDownloads.Rows)
        {
            if (row.Tag is not DownloadItem item) continue;
            row.Cells["colSelect"].Value = item.Selected;
            row.Cells["colProgress"].Value = FormatProgress(item);
            row.Cells["colSpeed"].Value = FormatSpeed(item);
            row.Cells["colStatus"].Value = item.Status.ToString();
        }
    }

    // ── Formatting ──

    private static string FormatProgress(DownloadItem item)
    {
        if (item.TotalBytes <= 0) return "---";
        double pct = item.BytesReceived * 100.0 / item.TotalBytes;
        return $"{FormatSize(item.BytesReceived)} / {FormatSize(item.TotalBytes)} ({pct:F1}%)";
    }

    private static string FormatSpeed(DownloadItem item)
    {
        if (item.Status != DownloadStatus.Downloading || item.SpeedBps <= 0)
            return "---";
        return $"{FormatSize((long)item.SpeedBps)}/s";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "---";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    // ── State ──

    private void SetRunningState(bool running)
    {
        _isRunning = running;
        btnParse.Enabled = !running;
        btnStart.Enabled = !running;
        btnCancel.Enabled = running;
        btnPauseAll.Enabled = running;
        btnResumeAll.Enabled = running;
        txtInput.ReadOnly = running;
        txtSavePath.ReadOnly = running;
        btnBrowse.Enabled = !running;
        nudMaxParallel.Enabled = !running;
        nudDelay.Enabled = !running;
    }

    private void Log(string msg)
    {
        string ts = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{ts}] {msg}{Environment.NewLine}");
    }
}
