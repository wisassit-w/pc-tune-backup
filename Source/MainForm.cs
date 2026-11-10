using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcTuneBackup
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(15, 17, 23);
        public static readonly Color Card = Color.FromArgb(24, 27, 36);
        public static readonly Color CardHover = Color.FromArgb(31, 35, 47);
        public static readonly Color Border = Color.FromArgb(42, 47, 62);
        public static readonly Color Text = Color.FromArgb(232, 234, 240);
        public static readonly Color Sub = Color.FromArgb(138, 145, 163);
        public static readonly Color LogText = Color.FromArgb(185, 191, 207);
        public static readonly Color Accent = Color.FromArgb(124, 92, 255);
        public static readonly Color Accent2 = Color.FromArgb(0, 200, 255);
        public static readonly Color Ok = Color.FromArgb(70, 210, 140);
        public static readonly Color Err = Color.FromArgb(255, 95, 110);
        public static readonly Color Warn = Color.FromArgb(255, 200, 80);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            int d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color ParentBg(Control c)
        {
            return c.Parent != null ? c.Parent.BackColor : Bg;
        }
    }

    class ToggleRow : Control
    {
        static readonly Font HeadFont = new Font("Segoe UI Semibold", 11f);
        static readonly Font DescFont = new Font("Segoe UI", 9f);
        bool hover;

        public string Heading { get; set; }
        public string Desc { get; set; }
        public bool On { get; set; }

        public ToggleRow()
        {
            Heading = "";
            Desc = "";
            On = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { On = !On; Invalidate(); base.OnClick(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.ParentBg(this));

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.Round(rect, 12))
            {
                using (var b = new SolidBrush(hover && Enabled ? Theme.CardHover : Theme.Card)) g.FillPath(b, path);
                using (var pen = new Pen(On ? Color.FromArgb(150, Theme.Accent) : Theme.Border, 1f)) g.DrawPath(pen, path);
            }

            int textW = Width - 18 - 44 - 18 - 14;
            int y0 = (Height - 40) / 2;
            Color fore = Enabled ? Theme.Text : Theme.Sub;
            var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Heading, HeadFont, new Rectangle(18, y0, textW, 21), fore, flags);
            TextRenderer.DrawText(g, Desc, DescFont, new Rectangle(18, y0 + 22, textW, 18), Theme.Sub, flags);

            var tr = new Rectangle(Width - 18 - 44, (Height - 24) / 2, 44, 24);
            using (var tp = Theme.Round(tr, 12))
            {
                if (On)
                {
                    using (var gb = new LinearGradientBrush(tr, Theme.Accent, Theme.Accent2, 0f)) g.FillPath(gb, tp);
                }
                else
                {
                    using (var sb = new SolidBrush(Color.FromArgb(58, 63, 80))) g.FillPath(sb, tp);
                }
            }
            int kx = On ? tr.Right - 21 : tr.X + 3;
            using (var kb = new SolidBrush(Color.White)) g.FillEllipse(kb, kx, tr.Y + 3, 18, 18);
        }
    }

    class FlatBtn : Control
    {
        static readonly Font F = new Font("Segoe UI Semibold", 11f);
        bool hover, down;

        public bool Primary { get; set; }

        public FlatBtn()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.ParentBg(this));

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.Round(rect, 12))
            {
                if (!Enabled)
                {
                    using (var db = new SolidBrush(Color.FromArgb(34, 38, 50))) g.FillPath(db, path);
                }
                else if (Primary)
                {
                    using (var gb = new LinearGradientBrush(rect, Theme.Accent, Theme.Accent2, 0f)) g.FillPath(gb, path);
                }
                else
                {
                    using (var cb = new SolidBrush(Theme.Card)) g.FillPath(cb, path);
                    using (var pen = new Pen(Color.FromArgb(160, Theme.Accent), 1.5f)) g.DrawPath(pen, path);
                }

                if (Enabled && hover && !down)
                    using (var ob = new SolidBrush(Color.FromArgb(28, 255, 255, 255))) g.FillPath(ob, path);
                if (Enabled && down)
                    using (var ob2 = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillPath(ob2, path);
            }

            TextRenderer.DrawText(g, Text, F, rect, Enabled ? Color.White : Theme.Sub,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }

    class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.ParentBg(this));
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.Round(rect, 12))
            {
                using (var b = new SolidBrush(Theme.Card)) g.FillPath(b, path);
                using (var pen = new Pen(Theme.Border, 1f)) g.DrawPath(pen, path);
            }
        }
    }

    class BusyBar : Control
    {
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        float x = -140;

        public BusyBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 4;
            timer.Interval = 16;
            timer.Tick += delegate
            {
                x += 8;
                if (x > Width) x = -140;
                Invalidate();
            };
        }

        public void Start() { x = -140; timer.Start(); Invalidate(); }
        public void Stop() { timer.Stop(); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.ParentBg(this));
            using (var tb = new SolidBrush(Theme.Border)) g.FillRectangle(tb, 0, 0, Width, Height);
            if (!timer.Enabled) return;
            var seg = new Rectangle((int)x, 0, 140, Height);
            using (var gb = new LinearGradientBrush(seg, Theme.Accent, Theme.Accent2, 0f))
                g.FillRectangle(gb, seg);
        }
    }

    class Row
    {
        public ToggleRow Ctl;
        public ISettingsModule Mod;
    }

    class MainForm : Form
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        readonly FileModule files = new FileModule();
        readonly ISettingsModule[] modules;
        readonly List<Row> rows = new List<Row>();

        readonly FlatBtn bExport = new FlatBtn();
        readonly FlatBtn bRestore = new FlatBtn();
        readonly FlatBtn bAdd = new FlatBtn();
        readonly ToggleRow rowPoint = new ToggleRow();
        readonly RichTextBox logBox = new RichTextBox();
        readonly BusyBar bar = new BusyBar();
        readonly Label lblStatus = new Label();
        Label lblSub;
        string embedded;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x20000;       // WS_MINIMIZEBOX
                cp.ClassStyle |= 0x20000;  // CS_DROPSHADOW
                return cp;
            }
        }

        public MainForm()
        {
            Text = "PC Tune Backup";
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(940, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            modules = new ISettingsModule[]
            {
                new RegistryModule(), new NetshModule(), new AdapterModule(),
                new PowerPlanModule(), new MsiModeModule(), files
            };

            bExport.Text = "Export (Backup)"; bExport.Primary = true;
            bRestore.Text = "Restore";
            bAdd.Text = "+  Add game file...";
            rowPoint.Heading = "Create restore point";
            rowPoint.Desc = "Creates a Windows restore point before Restore";
            rowPoint.On = true;

            // ---- title bar ----
            var title = new Label();
            title.Text = "PC Tune Backup"; title.AutoSize = true; title.Location = new Point(24, 12);
            title.Font = new Font("Segoe UI Semibold", 15f); title.ForeColor = Theme.Text; title.BackColor = Color.Transparent;
            var sub = lblSub = new Label();
            sub.Text = "FiveM optimization profile  -  Export and Restore"; sub.AutoSize = true;
            sub.Location = new Point(26, 40); sub.Font = new Font("Segoe UI", 9f);
            sub.ForeColor = Theme.Sub; sub.BackColor = Color.Transparent;
            title.MouseDown += Drag; sub.MouseDown += Drag; MouseDown += Drag;

            var bClose = WinBtn("\u2715", ClientSize.Width - 52, Color.FromArgb(232, 65, 85));
            bClose.Click += delegate { Close(); };
            var bMin = WinBtn("\u2014", ClientSize.Width - 98, Theme.CardHover);
            bMin.Click += delegate { WindowState = FormWindowState.Minimized; };

            // ---- left: categories ----
            var lblCat = Section("CATEGORIES", 24, 84);
            var info = new Dictionary<string, string[]>();
            info["registry"] = new[] { "Input Lag and Registry", "Win32PrioritySeparation, Mouse/Keyboard queue, HAGS, Game Mode" };
            info["netsh"] = new[] { "Network Stack (Netsh)", "TCP auto-tuning and all netsh interface settings" };
            info["adapters"] = new[] { "LAN Adapter Driver", "Buffers, RSS, power saving of your network card" };
            info["powerplan"] = new[] { "Power Plan", "Ultimate Performance and plan details" };
            info["msi"] = new[] { "MSI Mode", "MSI settings for GPU / LAN / PCI devices" };
            info["files"] = new[] { "FiveM / GTA Files", "commandline.txt, gta5_settings.xml, CitizenFX.ini" };
            Controls.AddRange(new Control[] { title, sub, bMin, bClose, lblCat });

            int y = 118;
            foreach (var m in modules)
            {
                var row = new ToggleRow();
                row.Heading = info[m.Key][0];
                row.Desc = info[m.Key][1];
                row.On = true;
                row.Location = new Point(24, y);
                row.Size = new Size(430, 66);
                rows.Add(new Row { Ctl = row, Mod = m });
                Controls.Add(row);
                y += 74;
            }

            // ---- right: actions ----
            int rx = 478, rw = 438;
            var lblAct = Section("ACTIONS", rx, 84);
            bExport.SetBounds(rx, 114, 213, 54);
            bRestore.SetBounds(rx + 225, 114, 213, 54);
            bAdd.SetBounds(rx, 180, rw, 42);
            rowPoint.SetBounds(rx, 234, rw, 60);

            var lblLog = Section("ACTIVITY", rx, 312);
            var logCard = new CardPanel();
            logCard.Location = new Point(rx, 342);
            logCard.Size = new Size(rw, 222);
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = Theme.Card;
            logBox.ForeColor = Theme.LogText;
            logBox.Font = new Font("Consolas", 9.5f);
            logBox.ReadOnly = true;
            logBox.DetectUrls = false;
            logBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            logBox.SetBounds(14, 12, rw - 28, 222 - 24);
            logCard.Controls.Add(logBox);

            Controls.AddRange(new Control[] { lblAct, bExport, bRestore, bAdd, rowPoint, lblLog, logCard });

            // ---- bottom ----
            bar.SetBounds(24, 584, ClientSize.Width - 48, 4);
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(24, 604);
            lblStatus.Font = new Font("Segoe UI", 9f);
            lblStatus.ForeColor = Theme.Sub;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Text = "Ready";
            var lblVer = new Label();
            lblVer.Text = "Niran Shop  v3.1"; lblVer.AutoSize = true; lblVer.Font = new Font("Segoe UI", 9f);
            lblVer.ForeColor = Theme.Sub; lblVer.BackColor = Color.Transparent;
            lblVer.Location = new Point(ClientSize.Width - 130, 604);
            Controls.AddRange(new Control[] { bar, lblStatus, lblVer });

            bExport.Click += OnExport;
            bRestore.Click += OnRestore;
            bAdd.Click += OnAddFile;

            Log("Ready. Choose categories, then Export or Restore.");

            embedded = Embedded.ReadOwn();
            if (embedded != null) SetupEmbedded();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int pref = 2; DwmSetWindowAttribute(Handle, 33, ref pref, 4); } catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var gb = new LinearGradientBrush(new Rectangle(0, 0, Width, 3), Theme.Accent, Theme.Accent2, 0f))
                e.Graphics.FillRectangle(gb, 0, 0, Width, 3);
        }

        static Label Section(string text, int x, int y)
        {
            var l = new Label();
            l.Text = text; l.AutoSize = true; l.Location = new Point(x, y);
            l.Font = new Font("Segoe UI Semibold", 9f); l.ForeColor = Theme.Sub; l.BackColor = Color.Transparent;
            return l;
        }

        static Label WinBtn(string text, int x, Color hoverColor)
        {
            var l = new Label();
            l.Text = text; l.Size = new Size(40, 32); l.Location = new Point(x, 14);
            l.TextAlign = ContentAlignment.MiddleCenter; l.Font = new Font("Segoe UI Symbol", 10f);
            l.ForeColor = Theme.Text; l.BackColor = Theme.Bg; l.Cursor = Cursors.Hand;
            l.MouseEnter += delegate { l.BackColor = hoverColor; };
            l.MouseLeave += delegate { l.BackColor = Theme.Bg; };
            return l;
        }

        void Drag(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < 70)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        void Log(string s)
        {
            if (InvokeRequired) { BeginInvoke(new Action(delegate { Log(s); })); return; }
            Color c = Theme.LogText;
            string t = s.TrimStart();
            if (s.StartsWith("[Export]") || s.StartsWith("[Restore]")) c = Theme.Accent2;
            else if (t.StartsWith("!") || s.Contains("FAIL") || s.StartsWith("ERROR")) c = Theme.Err;
            else if (s.StartsWith("Saved") || s.StartsWith("Done") || s.StartsWith("Ready")) c = Theme.Ok;
            else if (s.StartsWith("[Skip]")) c = Theme.Warn;

            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = c;
            logBox.AppendText(s + "\n");
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }

        void SetBusy(bool busy, string status)
        {
            bExport.Enabled = bRestore.Enabled = bAdd.Enabled = !busy;
            foreach (var r in rows) r.Ctl.Enabled = !busy;
            rowPoint.Enabled = !busy;
            lblStatus.Text = status;
            if (busy) bar.Start(); else bar.Stop();
        }

        List<ISettingsModule> Selected()
        {
            return rows.Where(r => r.Ctl.On).Select(r => r.Mod).ToList();
        }

        void OnAddFile(object s, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select game files to include";
                dlg.Multiselect = true;
                if (dlg.ShowDialog() != DialogResult.OK) return;
                foreach (var f in dlg.FileNames)
                {
                    files.ExtraPaths.Add(f);
                    Log("Added: " + f);
                }
            }
        }

        void SetupEmbedded()
        {
            try
            {
                var root = Json.O(Json.Parse(embedded));
                var mods = Json.O(Json.Get(root, "modules"));
                lblSub.Text = "Profile inside this EXE  -  from " + Json.S(Json.Get(root, "machine"));
                bRestore.Text = "Apply Profile";
                bRestore.Primary = true; bRestore.Invalidate();
                bExport.Primary = false; bExport.Invalidate();
                Log("Profile loaded from this EXE");
                Log("  created: " + Json.S(Json.Get(root, "createdAt")));
                foreach (var r in rows)
                {
                    bool has = Json.Get(mods, r.Mod.Key) != null;
                    r.Ctl.On = has;
                    r.Ctl.Invalidate();
                    if (has) Log("  includes: " + r.Mod.Title);
                }
                Log("Press Apply Profile to set this PC.");
            }
            catch (Exception ex)
            {
                Log("ERROR: profile unreadable: " + ex.Message);
                embedded = null;
            }
        }

        async void OnExport(object s, EventArgs e)
        {
            string path;
            bool asExe;
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Single EXE - program + settings in one file (*.exe)|*.exe|Profile JSON (*.json)|*.json";
                dlg.FileName = "my-tune.exe";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                asExe = dlg.FilterIndex == 1;
                path = Path.ChangeExtension(dlg.FileName, asExe ? ".exe" : ".json");
            }
            if (asExe && string.Equals(Path.GetFullPath(path), Path.GetFullPath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please choose a different file name.", "PC Tune Backup");
                return;
            }
            var sel = Selected();

            SetBusy(true, "Exporting...");
            await Task.Run(delegate
            {
                try
                {
                    var mods = new Dictionary<string, object>();
                    foreach (var m in sel)
                    {
                        Log("[Export] " + m.Title);
                        try { mods[m.Key] = m.Export(Log); }
                        catch (Exception ex) { Log("  FAIL: " + ex.Message); }
                    }
                    var root = new Dictionary<string, object>();
                    root["version"] = 2L;
                    root["machine"] = Environment.MachineName;
                    root["createdAt"] = DateTime.UtcNow.ToString("o");
                    root["modules"] = mods;
                    string json = Json.Write(root);
                    if (asExe)
                    {
                        Embedded.WriteExe(path, json);
                        Log("Saved: " + path);
                        Log("  one file = program + your settings");
                    }
                    else
                    {
                        File.WriteAllText(path, json, new UTF8Encoding(false));
                        Log("Saved: " + path);
                    }
                }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
            });
            SetBusy(false, "Export finished");
        }

        async void OnRestore(object s, EventArgs e)
        {
            string jsonText;
            if (embedded != null) jsonText = embedded;
            else
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Tune profile (*.json;*.exe)|*.json;*.exe";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    try
                    {
                        jsonText = dlg.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                            ? Embedded.ReadFrom(dlg.FileName)
                            : File.ReadAllText(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "PC Tune Backup");
                        return;
                    }
                    if (jsonText == null)
                    {
                        MessageBox.Show("This EXE has no saved profile inside.", "PC Tune Backup");
                        return;
                    }
                }
            }
            var sel = Selected();
            bool point = rowPoint.On;

            if (MessageBox.Show("Apply these settings to this PC now?", "Confirm Restore",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            SetBusy(true, "Restoring...");
            await Task.Run(delegate
            {
                try
                {
                    if (point)
                    {
                        Log("Creating restore point...");
                        try
                        {
                            Shell.Run("powershell",
                                "-NoProfile -Command \"Checkpoint-Computer -Description 'PcTuneBackup' -RestorePointType MODIFY_SETTINGS\"");
                        }
                        catch (Exception ex) { Log("  ! restore point failed: " + ex.Message); }
                    }
                    var root = Json.O(Json.Parse(jsonText));
                    var mods = Json.O(Json.Get(root, "modules"));
                    foreach (var m in sel)
                    {
                        object data = Json.Get(mods, m.Key);
                        if (data == null) { Log("[Skip] " + m.Title + " (not in profile)"); continue; }
                        Log("[Restore] " + m.Title);
                        try { m.Restore(data, Log); }
                        catch (Exception ex) { Log("  FAIL: " + ex.Message); }
                    }
                    Log("Done. Please restart Windows.");
                }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
            });
            SetBusy(false, "Restore finished - restart Windows");
        }
    }
}
