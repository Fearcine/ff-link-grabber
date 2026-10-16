namespace FFGrabber;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private SplitContainer splitTop;
    private Label lblInput;
    private TextBox txtInput;
    private Label lblBrowser;
    private Panel pnlBrowser;
    private Microsoft.Web.WebView2.WinForms.WebView2 webView;

    private Button btnParse;
    private Label lblSaveTo;
    private TextBox txtSavePath;
    private Button btnBrowse;

    private Label lblMaxParallel;
    private NumericUpDown nudMaxParallel;
    private Label lblDelay;
    private NumericUpDown nudDelay;
    private Label lblMs;
    private Button btnStart;
    private Button btnPauseAll;
    private Button btnResumeAll;
    private Button btnCancel;

    private DataGridView dgvDownloads;
    private ContextMenuStrip ctxMenu;
    private ToolStripMenuItem ctxPause;
    private ToolStripMenuItem ctxResume;
    private ToolStripMenuItem ctxSkip;

    private Label lblLog;
    private TextBox txtLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        splitTop = new SplitContainer();
        lblInput = new Label();
        txtInput = new TextBox();
        lblBrowser = new Label();
        pnlBrowser = new Panel();
        webView = new Microsoft.Web.WebView2.WinForms.WebView2();

        btnParse = new Button();
        lblSaveTo = new Label();
        txtSavePath = new TextBox();
        btnBrowse = new Button();

        lblMaxParallel = new Label();
        nudMaxParallel = new NumericUpDown();
        lblDelay = new Label();
        nudDelay = new NumericUpDown();
        lblMs = new Label();
        btnStart = new Button();
        btnPauseAll = new Button();
        btnResumeAll = new Button();
        btnCancel = new Button();

        dgvDownloads = new DataGridView();
        ctxMenu = new ContextMenuStrip();
        ctxPause = new ToolStripMenuItem();
        ctxResume = new ToolStripMenuItem();
        ctxSkip = new ToolStripMenuItem();

        lblLog = new Label();
        txtLog = new TextBox();

        ((System.ComponentModel.ISupportInitialize)splitTop).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxParallel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudDelay).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvDownloads).BeginInit();
        ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
        SuspendLayout();

        // ── SplitContainer (input | browser) ──
        splitTop.Location = new Point(0, 0);
        splitTop.Size = new Size(900, 130);
        splitTop.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        splitTop.Orientation = Orientation.Vertical;
        splitTop.SplitterDistance = 530;
        splitTop.Panel1MinSize = 200;
        splitTop.Panel2MinSize = 180;
        splitTop.SplitterWidth = 5;

        // Panel1: input
        lblInput.Text = "Input Links (one per line):";
        lblInput.Dock = DockStyle.Top;
        lblInput.Height = 20;
        lblInput.Padding = new Padding(4, 3, 0, 0);

        txtInput.Multiline = true;
        txtInput.ScrollBars = ScrollBars.Both;
        txtInput.WordWrap = false;
        txtInput.Dock = DockStyle.Fill;
        txtInput.Font = new Font("Consolas", 8.5F);

        splitTop.Panel1.Controls.Add(txtInput);
        splitTop.Panel1.Controls.Add(lblInput);

        // Panel2: browser
        lblBrowser.Text = "Browser:";
        lblBrowser.Dock = DockStyle.Top;
        lblBrowser.Height = 20;
        lblBrowser.Padding = new Padding(4, 3, 0, 0);

        pnlBrowser.Dock = DockStyle.Fill;
        pnlBrowser.BorderStyle = BorderStyle.FixedSingle;
        webView.Dock = DockStyle.Fill;
        pnlBrowser.Controls.Add(webView);

        splitTop.Panel2.Controls.Add(pnlBrowser);
        splitTop.Panel2.Controls.Add(lblBrowser);

        // ── Row 1: Parse + Save path ──
        btnParse.Location = new Point(10, 136);
        btnParse.Size = new Size(85, 26);
        btnParse.Text = "Parse Links";
        btnParse.FlatStyle = FlatStyle.System;

        lblSaveTo.AutoSize = true;
        lblSaveTo.Location = new Point(105, 142);
        lblSaveTo.Text = "Save to:";

        txtSavePath.Location = new Point(160, 138);
        txtSavePath.Size = new Size(570, 23);
        txtSavePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSavePath.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        btnBrowse.Location = new Point(736, 136);
        btnBrowse.Size = new Size(60, 26);
        btnBrowse.Text = "Browse";
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.FlatStyle = FlatStyle.System;

        // ── Row 2: Controls ──
        lblMaxParallel.AutoSize = true;
        lblMaxParallel.Location = new Point(10, 174);
        lblMaxParallel.Text = "Parallel:";

        nudMaxParallel.Location = new Point(65, 170);
        nudMaxParallel.Size = new Size(45, 23);
        nudMaxParallel.Minimum = 1;
        nudMaxParallel.Maximum = 10;
        nudMaxParallel.Value = 3;

        lblDelay.AutoSize = true;
        lblDelay.Location = new Point(120, 174);
        lblDelay.Text = "Delay:";

        nudDelay.Location = new Point(160, 170);
        nudDelay.Size = new Size(60, 23);
        nudDelay.Minimum = 300;
        nudDelay.Maximum = 10000;
        nudDelay.Value = 1500;
        nudDelay.Increment = 100;

        lblMs.AutoSize = true;
        lblMs.Location = new Point(224, 174);
        lblMs.Text = "ms";

        btnStart.Location = new Point(280, 168);
        btnStart.Size = new Size(70, 26);
        btnStart.Text = "▶ Start";
        btnStart.FlatStyle = FlatStyle.System;

        btnPauseAll.Location = new Point(356, 168);
        btnPauseAll.Size = new Size(80, 26);
        btnPauseAll.Text = "⏸ Pause All";
        btnPauseAll.FlatStyle = FlatStyle.System;
        btnPauseAll.Enabled = false;

        btnResumeAll.Location = new Point(442, 168);
        btnResumeAll.Size = new Size(90, 26);
        btnResumeAll.Text = "▶ Resume All";
        btnResumeAll.FlatStyle = FlatStyle.System;
        btnResumeAll.Enabled = false;

        btnCancel.Location = new Point(538, 168);
        btnCancel.Size = new Size(65, 26);
        btnCancel.Text = "■ Cancel";
        btnCancel.FlatStyle = FlatStyle.System;
        btnCancel.Enabled = false;

        // ── DataGridView ──
        dgvDownloads.Location = new Point(10, 200);
        dgvDownloads.Size = new Size(880, 360);
        dgvDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        dgvDownloads.AllowUserToAddRows = false;
        dgvDownloads.AllowUserToDeleteRows = false;
        dgvDownloads.AllowUserToResizeRows = false;
        dgvDownloads.RowHeadersVisible = false;
        dgvDownloads.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDownloads.MultiSelect = false;
        dgvDownloads.ReadOnly = false;
        dgvDownloads.BackgroundColor = SystemColors.Window;
        dgvDownloads.BorderStyle = BorderStyle.FixedSingle;
        dgvDownloads.Font = new Font("Segoe UI", 8.5F);
        dgvDownloads.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
        dgvDownloads.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

        var colSelect = new DataGridViewCheckBoxColumn();
        colSelect.Name = "colSelect";
        colSelect.HeaderText = "✓";
        colSelect.Width = 30;
        colSelect.FalseValue = false;
        colSelect.TrueValue = true;

        var colPack = new DataGridViewTextBoxColumn();
        colPack.Name = "colPack";
        colPack.HeaderText = "Pack";
        colPack.Width = 175;
        colPack.ReadOnly = true;

        var colFile = new DataGridViewTextBoxColumn();
        colFile.Name = "colFile";
        colFile.HeaderText = "Filename";
        colFile.Width = 190;
        colFile.ReadOnly = true;

        var colProgress = new DataGridViewTextBoxColumn();
        colProgress.Name = "colProgress";
        colProgress.HeaderText = "Progress";
        colProgress.Width = 185;
        colProgress.ReadOnly = true;

        var colSpeed = new DataGridViewTextBoxColumn();
        colSpeed.Name = "colSpeed";
        colSpeed.HeaderText = "Speed";
        colSpeed.Width = 85;
        colSpeed.ReadOnly = true;

        var colStatus = new DataGridViewTextBoxColumn();
        colStatus.Name = "colStatus";
        colStatus.HeaderText = "Status";
        colStatus.Width = 80;
        colStatus.ReadOnly = true;

        dgvDownloads.Columns.AddRange(colSelect, colPack, colFile, colProgress, colSpeed, colStatus);

        // ── Context menu ──
        ctxPause.Text = "Pause";
        ctxResume.Text = "Resume";
        ctxSkip.Text = "Skip / Cancel";
        ctxMenu.Items.AddRange(new ToolStripItem[] { ctxPause, ctxResume, new ToolStripSeparator(), ctxSkip });
        dgvDownloads.ContextMenuStrip = ctxMenu;

        // ── Log ──
        lblLog.AutoSize = true;
        lblLog.Location = new Point(10, 566);
        lblLog.Text = "Log:";
        lblLog.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        txtLog.Location = new Point(10, 584);
        txtLog.Size = new Size(880, 56);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.WordWrap = true;
        txtLog.BackColor = SystemColors.Window;
        txtLog.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        txtLog.Font = new Font("Consolas", 8F);

        // ── Form ──
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 650);
        MinimumSize = new Size(700, 520);
        Text = "FF Link Grabber";
        Font = new Font("Segoe UI", 9F);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(splitTop);
        Controls.Add(btnParse);
        Controls.Add(lblSaveTo);
        Controls.Add(txtSavePath);
        Controls.Add(btnBrowse);
        Controls.Add(lblMaxParallel);
        Controls.Add(nudMaxParallel);
        Controls.Add(lblDelay);
        Controls.Add(nudDelay);
        Controls.Add(lblMs);
        Controls.Add(btnStart);
        Controls.Add(btnPauseAll);
        Controls.Add(btnResumeAll);
        Controls.Add(btnCancel);
        Controls.Add(dgvDownloads);
        Controls.Add(lblLog);
        Controls.Add(txtLog);

        ((System.ComponentModel.ISupportInitialize)splitTop).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxParallel).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudDelay).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvDownloads).EndInit();
        ((System.ComponentModel.ISupportInitialize)webView).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
