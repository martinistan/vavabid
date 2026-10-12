using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace VakantieVeilingenBieder
{
    public sealed class MainForm : Form
    {
        const string DefaultHome = "https://www.vakantieveilingen.be/";
        static readonly Color BgDark = Color.Black;
        static readonly Color BgSidebar = Color.Black;
        static readonly Color BgSurface = Color.FromArgb(18, 18, 22);
        static readonly Color BgRaised = Color.FromArgb(28, 28, 34);
        static readonly Color BorderColor = Color.FromArgb(39, 39, 46);
        static readonly Color AccentEmerald = Color.FromArgb(16, 185, 129);
        static readonly Color AccentAmber = Color.FromArgb(245, 158, 11);
        static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);
        static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        static readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        static readonly Color TextMuted = Color.FromArgb(161, 161, 170);

        readonly string profilePath = @"C:\VakantieVeilingenBot\Profile";
        readonly string screenshotsPath = @"C:\VakantieVeilingenBot\Screenshots";
        readonly JavaScriptSerializer json = new JavaScriptSerializer();
        readonly List<PageItem> pages = new List<PageItem>();

        // Layout controls
        readonly Panel topHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgDark, Padding = new Padding(0), Margin = new Padding(0) };
        bool isSettingsActive = false;
        bool isTerminalActive, controlsExpanded, closeConfirmed;
        WebView2 terminalBrowser, dialogBrowser;
        readonly Panel terminalView = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Visible = false };
        readonly Panel dialogView = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Visible = false };
        readonly List<object> logHistory = new List<object>();
        PageItem dialogPage;
        string dialogMode;

        // Modern Chromium WebView2 Panels (100% HTML5/CSS3 Luxury UI)
        WebView2 topbarBrowser;
        WebView2 sidebarBrowser;
        WebView2 settingsBrowser;
        readonly Panel toolDock = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Black, Padding = new Padding(0), Margin = new Padding(0) };

        // Left Sidebar (Luxury Cyber HUD & Terminal)
        readonly Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = BgSidebar, Padding = new Padding(0) };
        readonly Splitter sidebarSplitter = new Splitter { Dock = DockStyle.Left, Width = 8, BackColor = Color.FromArgb(18, 24, 23), Cursor = Cursors.VSplit, TabStop = false };
        int windowDpi = 96;
        double preferredSidebarWidth = 340;
        bool applyingLayout;
        DateTime lastCaptionClick = DateTime.MinValue;
        Point lastCaptionPoint;
        
        // Quick Stepper Controls (Defaults: Max 32, Step 1, Seconds 1)
        readonly SleekNumericBox numMax = new SleekNumericBox(1, 100000, 32, 5, " €");
        readonly SleekNumericBox numStep = new SleekNumericBox(1, 1000, 1, 1, " €");
        readonly SleekNumericBox numSeconds = new SleekNumericBox(1, 60, 1, 1, " sn");
        readonly CheckBox chkStopOnWin = new CheckBox { Text = "İlk kazançta diğerlerini durdur", AutoSize = true, Checked = true, ForeColor = Color.Gainsboro, Font = new Font("Segoe UI", 8.5f), Margin = new Padding(2, 6, 2, 6) };
        readonly Button btnCopyUrl = CreateButton("🔗 URL Kopyala", 154, 32, BgRaised);
        readonly Button btnMobileQR = CreateButton("📱 Mobil Link", 154, 32, BgRaised);

        // System & Sniper Terminal (CMD Console)
        RichTextBox terminalBox;

        // Main Center Area (WebView2 & Settings)
        readonly Panel webContainer = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(0) };
        readonly Panel webCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

        // Entegre In-App Ayarlar Paneli
        readonly Panel pnlSettingsView = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Visible = false, Padding = new Padding(0) };
        TextBox txtDiscordToken;
        TextBox txtGuildId;
        CheckBox chkWhatsAppEnabled;
        TextBox txtWhatsAppPhone;
        TextBox txtWhatsAppApiKey;
        Button btnTestWhatsApp;
        Label lblTestWaResult;
        TextBox txtWebPin;
        TextBox txtWebPort;
        Label lblWebUrlDisplay;
        Button btnSaveSettings;
        Button btnBackToAuctions;
        Label lblSettingsStatus;

        readonly System.Windows.Forms.Timer pollTimer = new System.Windows.Forms.Timer { Interval = 250 };
        PageItem current;
        CoreWebView2Environment environment;
        HttpListener ipcListener;
        bool busy, closing, syncing;
        string adapterScript, darkCss;
        string currentWebPort = "5000";
        readonly bool uiTestMode;
        string shutdownMode = "none";
        bool shutdownTriggered;

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] struct MinMaxInfo { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }

        public MainForm() : this(false) { }

        internal MainForm(bool testMode)
        {
            uiTestMode = testMode;
            if (uiTestMode) {
                profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-profile");
                screenshotsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-captures");
            }
            Text = "VakantieVeilingen Bieder - Umutistan";
            Font = new Font("Segoe UI", 9.5f);
            ForeColor = Color.White;
            BackColor = BgDark;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            DoubleBuffered = true;
            Padding = new Padding(0);
            Margin = new Padding(0);

            try {
                if (File.Exists("app-logo.ico")) Icon = new Icon("app-logo.ico");
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            } catch { }

            BuildLayout();
            BuildSettingsView();
            if (!uiTestMode) { LoadConfigToState(); SetupIpcServer(); }

            pollTimer.Tick += PollPages;
            Load += OnLoaded;
            FormClosing += OnClosing;
            SizeChanged += delegate { ApplyResponsiveLayout(); };
            sidebarSplitter.SplitterMoved += delegate {
                if (!applyingLayout) preferredSidebarWidth = sidebar.Width * 96.0 / windowDpi;
                SyncChromeLayout();
            };
            sidebarSplitter.DoubleClick += delegate { preferredSidebarWidth = 340; ApplyResponsiveLayout(); };
        }

        int Dip(double value) { return (int)Math.Round(value * windowDpi / 96.0); }

        protected override void OnLoad(EventArgs e)
        {
            try { windowDpi = (int)GetDpiForWindow(Handle); } catch { using (var g = CreateGraphics()) windowDpi = (int)g.DpiX; }
            if (windowDpi < 96) windowDpi = 96;
            Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
            MinimumSize = new Size(Math.Min(Dip(900), work.Width), Math.Min(Dip(520), work.Height));
            Size = new Size(Math.Min(Dip(1360), (int)(work.Width * 0.9)), Math.Min(Dip(860), (int)(work.Height * 0.88)));
            Location = new Point(work.Left + (work.Width - Width) / 2, work.Top + (work.Height - Height) / 2);
            ApplyResponsiveLayout();
            base.OnLoad(e);
        }

        void ApplyResponsiveLayout()
        {
            if (applyingLayout || topbarBrowser == null || ClientSize.Width == 0) return;
            applyingLayout = true;
            SuspendLayout();
            try {
                Padding = new Padding(0);
                topHeader.Height = Dip(48);
                toolDock.Height = Dip(48);
                sidebar.Visible = false;
                sidebarSplitter.Visible = false;
                sidebar.Width = 0;
                sidebarSplitter.Width = 0;
            } finally { ResumeLayout(true); applyingLayout = false; }
            SyncChromeLayout();
        }

        void SyncChromeLayout()
        {
            if (topbarBrowser == null || topbarBrowser.CoreWebView2 == null) return;
            topbarBrowser.CoreWebView2.ExecuteScriptAsync("document.documentElement.style.setProperty('--sidebar-width','140px');document.body.classList.add('compact-brand');");
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try {
                int dark = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 11 / 10 20H1+)
                DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
                // Fallback for Windows 10 1809+
                DwmSetWindowAttribute(Handle, 19, ref dark, sizeof(int));
                int border = 0x00000000;
                // DWMWA_BORDER_COLOR (Windows 11): 0x00000000 removes white/light border completely
                DwmSetWindowAttribute(Handle, 34, ref border, sizeof(int));
                DwmSetWindowAttribute(Handle, 35, ref border, sizeof(int));
            } catch { }
        }

        protected override CreateParams CreateParams
        {
            get {
                var cp = base.CreateParams;
                cp.Style |= 0x00040000 | 0x00020000 | 0x00010000 | 0x00080000; // Resizable frame + native system menu
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0024) {
                base.WndProc(ref m);
                var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
                if (GetMonitorInfo(MonitorFromWindow(Handle, 2), ref info)) {
                    var limits = (MinMaxInfo)Marshal.PtrToStructure(m.LParam, typeof(MinMaxInfo));
                    limits.MaxPosition = new Point(info.Work.Left - info.Monitor.Left, info.Work.Top - info.Monitor.Top);
                    limits.MaxSize = new Point(info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
                    limits.MinTrackSize = new Point(Math.Min(Dip(800), limits.MaxSize.X), Math.Min(Dip(460), limits.MaxSize.Y));
                    Marshal.StructureToPtr(limits, m.LParam, false);
                }
                return;
            }
            if (m.Msg == 0x02E0) {
                windowDpi = (int)(m.WParam.ToInt64() & 0xffff);
                var rect = (NativeRect)Marshal.PtrToStructure(m.LParam, typeof(NativeRect));
                MinimumSize = Size.Empty;
                Bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                ApplyResponsiveLayout();
                m.Result = IntPtr.Zero;
                return;
            }
            // WM_NCCALCSIZE (0x0083): Pencere cercevesi hesaplamasini sifirlayarak 1px beyaz cizgiyi tamamen yok eder
            if (m.Msg == 0x0083)
            {
                m.Result = IntPtr.Zero;
                return;
            }
            // WM_NCPAINT (0x0085): DWM / non-client cerceve cizimini sifirlayarak beyaz dis cizgiyi yok eder
            if (m.Msg == 0x0085)
            {
                m.Result = IntPtr.Zero;
                return;
            }
            // WM_NCACTIVATE (0x0086): Aktif/pasif cerceve cizim degisimini engeller
            if (m.Msg == 0x0086)
            {
                m.Result = (IntPtr)1;
                return;
            }

            // Kenarlardan boyutlandirma (Resize grip)
            if (m.Msg == 0x84 && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                long v = m.LParam.ToInt64();
                Point p = PointToClient(new Point((short)(v & 65535), (short)((v >> 16) & 65535)));
                int edge = Dip(6), corner = Dip(18);
                bool l = p.X < edge, r = p.X >= ClientSize.Width - edge, t = p.Y < edge, b = p.Y >= ClientSize.Height - edge;
                if (t || b) { l = p.X < corner; r = p.X >= ClientSize.Width - corner; }
                if (l || r) { t = p.Y < corner; b = p.Y >= ClientSize.Height - corner; }
                if (l || r || t || b)
                {
                    m.Result = (IntPtr)(t ? (l ? 13 : r ? 14 : 12) : b ? (l ? 16 : r ? 17 : 15) : l ? 10 : r ? 11 : 1);
                    return;
                }
                return;
            }

            base.WndProc(ref m);
        }

        public static bool ValidateAndNormalizeAuctionUrl(string rawUrl, out string normalizedUrl, out string errorMessage)
        {
            normalizedUrl = "";
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                errorMessage = "Lütfen bir ihale URL'si girin.";
                return false;
            }

            rawUrl = rawUrl.Trim().Trim('"', '\'', '`', '<', '>', ' ', '\t', '\r', '\n');
            string working = rawUrl;

            if (working.StartsWith("//"))
            {
                working = "https:" + working;
            }
            else if (working.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                working = "https://" + working.Substring(7);
            }
            else if (!working.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (working.StartsWith("www.vakantieveilingen.be", StringComparison.OrdinalIgnoreCase) ||
                    working.StartsWith("vakantieveilingen.be", StringComparison.OrdinalIgnoreCase) ||
                    working.StartsWith("www.vakantieveilingen.nl", StringComparison.OrdinalIgnoreCase) ||
                    working.StartsWith("vakantieveilingen.nl", StringComparison.OrdinalIgnoreCase))
                {
                    working = "https://" + working;
                }
                else if (working.StartsWith("/veilingen", StringComparison.OrdinalIgnoreCase))
                {
                    working = "https://www.vakantieveilingen.be" + working;
                }
                else if (working.StartsWith("veilingen", StringComparison.OrdinalIgnoreCase))
                {
                    working = "https://www.vakantieveilingen.be/" + working;
                }
                else
                {
                    errorMessage = "Yalnızca vakantieveilingen.be/veilingen/ ihale bağlantılarına izin verilir.";
                    return false;
                }
            }

            Uri uri;
            if (!Uri.TryCreate(working, UriKind.Absolute, out uri))
            {
                errorMessage = "Geçersiz URL formatı.";
                return false;
            }

            string host = uri.Host.ToLowerInvariant();
            bool validHost = host == "www.vakantieveilingen.be" || 
                             host == "vakantieveilingen.be" || 
                             host == "www.vakantieveilingen.nl" || 
                             host == "vakantieveilingen.nl";

            if (!validHost)
            {
                errorMessage = "Farklı sitelere izin verilmez! Sadece vakantieveilingen.be kabul edilir.";
                return false;
            }

            string normHost = host;
            if (host == "vakantieveilingen.be") normHost = "www.vakantieveilingen.be";
            else if (host == "vakantieveilingen.nl") normHost = "www.vakantieveilingen.nl";

            string path = uri.AbsolutePath;
            bool isVeiling = path.StartsWith("/veilingen", StringComparison.OrdinalIgnoreCase) || path == "/" || string.IsNullOrEmpty(path);

            if (!isVeiling)
            {
                errorMessage = "Sadece /veilingen/ altındaki ihale sayfalarına izin verilir.";
                return false;
            }

            string queryAndFragment = uri.Query + uri.Fragment;
            normalizedUrl = "https://" + normHost + path + queryAndFragment;
            return true;
        }

        public static string NormalizeAuctionUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return DefaultHome;
            string norm;
            string err;
            if (ValidateAndNormalizeAuctionUrl(url, out norm, out err))
                return norm;
            return DefaultHome;
        }

        void BuildLayout()
        {
            terminalBox = new RichTextBox();

            // --- 1. TOPBAR CHROMIUM WEBVIEW ---
            topbarBrowser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BgDark };
            topHeader.Controls.Add(topbarBrowser);

            // --- 2. TOP TOOL DOCK CHROMIUM WEBVIEW (HUD Toolbar) ---
            sidebarBrowser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BgSidebar };
            toolDock.Controls.Add(sidebarBrowser);

            // --- 3. SETTINGS & WEBVIEW CONTAINERS ---
            settingsBrowser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BgDark };
            pnlSettingsView.Controls.Add(settingsBrowser);
            terminalBrowser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
            terminalView.Controls.Add(terminalBrowser);
            dialogBrowser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
            dialogView.Controls.Add(dialogBrowser);

            webContainer.Controls.Add(pnlSettingsView);
            webContainer.Controls.Add(webCard);
            webContainer.Controls.Add(terminalView);

            // Assemble Form (Fill first, then toolDock, then topHeader, then dialogView)
            Controls.Add(webContainer);
            Controls.Add(toolDock);
            Controls.Add(topHeader);
            Controls.Add(dialogView);
        }

        public void AppendTerminal(string category, string message, Color color)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action(() => AppendTerminal(category, message, color)));
                return;
            }
            try {
                if (terminalBox != null && !terminalBox.IsDisposed) {
                    string time = DateTime.Now.ToString("HH:mm:ss");
                    terminalBox.SelectionStart = terminalBox.TextLength;
                    terminalBox.SelectionLength = 0;
                    terminalBox.SelectionColor = Color.FromArgb(100, 100, 110);
                    terminalBox.AppendText("[" + time + "] ");
                    terminalBox.SelectionColor = color;
                    terminalBox.AppendText("[" + category + "] ");
                    terminalBox.SelectionColor = Color.FromArgb(220, 220, 230);
                    terminalBox.AppendText(message + "\r\n");
                    terminalBox.ScrollToCaret();
                }

                var entry = new { type = category, message = message, time = DateTime.Now.ToString("HH:mm:ss") };
                logHistory.Add(entry);
                while (logHistory.Count > 500) logHistory.RemoveAt(0);
                if (terminalBrowser != null && terminalBrowser.CoreWebView2 != null)
                    terminalBrowser.CoreWebView2.ExecuteScriptAsync("if(window.appendEntry)window.appendEntry(" + json.Serialize(entry) + ");");
            } catch { }
        }

        void BuildSettingsView()
        {
            var pnlMain = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            var lblHeader = new Label {
                Text = "⚙️ SİSTEM, ENTEGRASYON VE BİLDİRİM AYARLARI",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = AccentEmerald,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 15)
            };
            pnlMain.Controls.Add(lblHeader);

            // 1. WhatsApp Card
            var cardWa = CreateSettingsCard("📱 WHATSAPP ANLIK BİLDİRİM ENTEGRASYONU");
            chkWhatsAppEnabled = new CheckBox {
                Text = "WhatsApp bildirimlerini aktif et (İhale kazanıldığında anında mesaj at)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            txtWhatsAppPhone = CreateStyledInput("+905xxxxxxxxx veya +316xxxxxxxx");
            txtWhatsAppApiKey = CreateStyledInput("CallMeBot API Anahtarı (Ücretsiz)");
            btnTestWhatsApp = CreateButton("💬 Test WhatsApp Bildirimi Gönder", 260, 34, AccentBlue);
            btnTestWhatsApp.Click += delegate { SendTestWhatsApp(); };
            lblTestWaResult = new Label { Text = "", AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = TextMuted, Margin = new Padding(0, 6, 0, 0) };

            cardWa.Controls.Add(chkWhatsAppEnabled);
            cardWa.Controls.Add(CreateSettingsLabel("Hedef WhatsApp Numarası (+ Ülke Koduyla):"));
            cardWa.Controls.Add(txtWhatsAppPhone);
            cardWa.Controls.Add(CreateSettingsLabel("CallMeBot API Key:"));
            cardWa.Controls.Add(txtWhatsAppApiKey);
            cardWa.Controls.Add(btnTestWhatsApp);
            cardWa.Controls.Add(lblTestWaResult);
            pnlMain.Controls.Add(cardWa);

            // 2. Discord Card
            var cardDiscord = CreateSettingsCard("🤖 DISCORD BOT & KANAL YÖNETİMİ");
            txtDiscordToken = CreateStyledInput("Discord Bot Token");
            txtDiscordToken.UseSystemPasswordChar = true;
            txtGuildId = CreateStyledInput("Discord Sunucu (Guild) ID");
            cardDiscord.Controls.Add(CreateSettingsLabel("Discord Bot Token:"));
            cardDiscord.Controls.Add(txtDiscordToken);
            cardDiscord.Controls.Add(CreateSettingsLabel("Sunucu (Guild) ID:"));
            cardDiscord.Controls.Add(txtGuildId);
            pnlMain.Controls.Add(cardDiscord);

            // 3. Web Dashboard & Güvenlik Card
            var cardWeb = CreateSettingsCard("🌐 MOBİL WEB DASHBOARD & TELEFON ERİŞİMİ");
            txtWebPin = CreateStyledInput("1234");
            txtWebPin.Width = 120;
            txtWebPin.MaxLength = 4;
            txtWebPort = CreateStyledInput("5000");
            txtWebPort.Width = 120;
            string localIp = GetLocalIpAddress();
            lblWebUrlDisplay = new Label {
                Text = "🌐 Canlı WiFi Web Paneli: http://" + localIp + ":" + currentWebPort,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            cardWeb.Controls.Add(CreateSettingsLabel("Mobil Giriş PIN Kodu (4 Haneli):"));
            cardWeb.Controls.Add(txtWebPin);
            cardWeb.Controls.Add(CreateSettingsLabel("Web Dashboard Port:"));
            cardWeb.Controls.Add(txtWebPort);
            cardWeb.Controls.Add(lblWebUrlDisplay);
            pnlMain.Controls.Add(cardWeb);

            // 4. Action Buttons Card
            var pnlActions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 15, 0, 30) };
            btnSaveSettings = CreateButton("💾 AYARLARI KAYDET", 180, 40, AccentEmerald);
            btnSaveSettings.Click += delegate { SaveSettingsFromUI(); };
            
            btnBackToAuctions = CreateButton("◀ İHALELERE DÖN", 160, 40, BgRaised);
            btnBackToAuctions.Click += delegate { 
                if (pages.Count > 0) SelectPage(current ?? pages[0]);
                else {
                    var t = AddPage(DefaultHome, null);
                }
            };

            lblSettingsStatus = new Label { Text = "", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(15, 10, 0, 0) };

            pnlActions.Controls.Add(btnSaveSettings);
            pnlActions.Controls.Add(btnBackToAuctions);
            pnlActions.Controls.Add(lblSettingsStatus);
            pnlMain.Controls.Add(pnlActions);

            pnlSettingsView.Controls.Add(pnlMain);
        }

        Panel CreateSettingsCard(string title)
        {
            var pnl = new FlowLayoutPanel {
                Width = 820,
                AutoSize = true,
                BackColor = BgSurface,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 14),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            pnl.Paint += delegate(object s, PaintEventArgs e) {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, BorderColor, ButtonBorderStyle.Solid);
            };

            var lbl = new Label {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AccentCyan,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        Label CreateSettingsLabel(string text)
        {
            return new Label {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.Silver,
                Margin = new Padding(0, 6, 0, 3)
            };
        }

        TextBox CreateStyledInput(string placeholder)
        {
            return new TextBox {
                Width = 460,
                Height = 32,
                BackColor = BgRaised,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        void SelectSettingsTab()
        {
            isSettingsActive = true;
            isTerminalActive = false;
            terminalView.Visible = false;
            foreach (var p in pages) {
                p.Browser.Visible = false;
            }

            webCard.Visible = false;
            pnlSettingsView.Visible = true;
            pnlSettingsView.BringToFront();
            settingsBrowser.BringToFront();

            SyncTabsToTopbar();

            if (settingsBrowser != null && settingsBrowser.CoreWebView2 != null) {
                string cfgFile = @"C:\VakantieVeilingenBot\settings.json";
                string raw = !uiTestMode && File.Exists(cfgFile) ? File.ReadAllText(cfgFile) : "{}";
                string localIp = GetLocalIpAddress();
                string script = string.Format("if (window.loadSettings) window.loadSettings({0}, '{1}');", raw, localIp);
                settingsBrowser.CoreWebView2.ExecuteScriptAsync(script);
            }
            AppendTerminal("SYSTEM", "Ayarlar paneli açıldı.", AccentCyan);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2) { ShowTabEditor(current); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void OnWorkspaceMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try {
                var message = json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (message != null && message.ContainsKey("action")) {
                    string action = Convert.ToString(message["action"]);
                    if (action == "rename_tab") ShowTabEditor(current);
                    else if (action == "nav_reload" || action == "reload") BeginInvoke(new Action(() => ReloadCurrentTab()));
                    else if (action == "nav_back" || action == "go_back") BeginInvoke(new Action(() => NavigateBack()));
                    else if (action == "nav_forward" || action == "go_forward") BeginInvoke(new Action(() => NavigateForward()));
                }
            } catch { }
        }

        void ReloadCurrentTab()
        {
            if (current != null && current.Browser != null && current.Browser.CoreWebView2 != null) {
                current.Browser.CoreWebView2.Reload();
                AppendTerminal("SYSTEM", "Sayfa yenilendi (F5 / Ctrl+R): " + Truncate(current.Title, 25), AccentCyan);
            }
        }

        void NavigateBack()
        {
            if (current != null && current.Browser != null && current.Browser.CoreWebView2 != null) {
                if (current.Browser.CoreWebView2.CanGoBack) {
                    current.Browser.CoreWebView2.GoBack();
                    AppendTerminal("SYSTEM", "Geri gidildi.", AccentCyan);
                }
            }
        }

        void NavigateForward()
        {
            if (current != null && current.Browser != null && current.Browser.CoreWebView2 != null) {
                if (current.Browser.CoreWebView2.CanGoForward) {
                    current.Browser.CoreWebView2.GoForward();
                    AppendTerminal("SYSTEM", "İleri gidildi.", AccentCyan);
                }
            }
        }

        async void HandleWorkspaceAction(string action)
        {
            if (action == "rename_tab") { ShowTabEditor(current); return; }
            if (action == "open_terminal") {
                isTerminalActive = true;
                isSettingsActive = false;
                webCard.Visible = false;
                pnlSettingsView.Visible = false;
                terminalView.Visible = true;
                terminalView.BringToFront();
                SyncTabsToTopbar();
                return;
            }
            if (action == "show_auctions") { if (current != null) SelectPage(current); return; }
            if (action == "home") {
                // Reuse an existing home tab; never navigate an armed auction away.
                var home = pages.FirstOrDefault(p => !p.Armed && p.Url.TrimEnd('/') == DefaultHome.TrimEnd('/'));
                if (home != null) SelectPage(home);
                else await AddPage(DefaultHome, null);
            }
        }

        void ShowTabEditor(PageItem page) { if (page != null) ShowDialog("rename", page); }

        async void ShowDialog(string mode, PageItem page)
        {
            if (dialogBrowser == null || dialogBrowser.CoreWebView2 == null) return;
            dialogMode = mode;
            dialogPage = page;
            dialogView.Visible = true;
            dialogView.BringToFront();
            var data = new {
                mode = mode,
                name = page != null ? (page.CustomTitle ?? page.Title ?? "İhale") : "",
                pinned = page != null && page.Pinned,
                count = pages.Count(p => p.Armed)
            };
            await dialogBrowser.CoreWebView2.ExecuteScriptAsync("window.showDialog(" + json.Serialize(data) + ");");
            dialogBrowser.Focus();
        }

        void OnDialogMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try {
                var data = json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                string action = Convert.ToString(data["action"]);
                if (action == "cancel") { dialogView.Visible = false; return; }
                if (action != "confirm" || !dialogView.Visible) return;
                dialogView.Visible = false;
                if (dialogMode == "rename" && dialogPage != null) {
                    string title = data.ContainsKey("name") ? Convert.ToString(data["name"]).Trim() : "";
                    dialogPage.CustomTitle = title.Length == 0 ? null : title.Substring(0, Math.Min(title.Length, 80));
                    dialogPage.Pinned = data.ContainsKey("pinned") && Convert.ToBoolean(data["pinned"]);
                    SyncTabsToTopbar();
                } else if (dialogMode == "close-tab" && dialogPage != null) {
                    RemovePage(dialogPage);
                } else if (dialogMode == "close-app") {
                    closeConfirmed = true;
                    Close();
                }
            } catch { }
        }

        void CopyCurrentAuctionUrl()
        {
            string url = current != null ? current.Url : "";
            if (!string.IsNullOrEmpty(url)) {
                try {
                    Clipboard.SetText(url);
                    AppendTerminal("INFO", "İhale URL panoya kopyalandı: " + url, AccentEmerald);
                } catch { }
            } else {
                AppendTerminal("ALERT", "Kopyalanacak aktif ihale URL bulunamadı.", AccentAmber);
            }
        }

        void CopyMobileLink()
        {
            string localIp = GetLocalIpAddress();
            string url = "http://" + localIp + ":" + currentWebPort;
            try {
                Clipboard.SetText(url);
                AppendTerminal("INFO", "Mobil web paneli linki kopyalandı: " + url, AccentCyan);
            } catch { }
        }

        void LoadConfigToState()
        {
            string cfgFile = @"C:\VakantieVeilingenBot\settings.json";
            string waEnabled = "0", waPhone = "", waApiKey = "", discToken = "", guild = "", pin = "1234", port = "5000";

            if (File.Exists(cfgFile)) {
                try {
                    string raw = File.ReadAllText(cfgFile);
                    var dict = json.Deserialize<Dictionary<string, object>>(raw);
                    if (dict.ContainsKey("whatsapp_enabled")) waEnabled = dict["whatsapp_enabled"].ToString();
                    if (dict.ContainsKey("whatsapp_target_phone")) waPhone = dict["whatsapp_target_phone"].ToString();
                    if (dict.ContainsKey("whatsapp_apikey")) waApiKey = dict["whatsapp_apikey"].ToString();
                    if (dict.ContainsKey("discord_token")) discToken = dict["discord_token"].ToString();
                    if (dict.ContainsKey("guild_id")) guild = dict["guild_id"].ToString();
                    if (dict.ContainsKey("web_pin")) pin = dict["web_pin"].ToString();
                    if (dict.ContainsKey("web_port")) port = dict["web_port"].ToString();
                } catch { }
            }

            currentWebPort = port;
            if (chkWhatsAppEnabled != null) chkWhatsAppEnabled.Checked = (waEnabled == "1");
            if (txtWhatsAppPhone != null) txtWhatsAppPhone.Text = waPhone;
            if (txtWhatsAppApiKey != null) txtWhatsAppApiKey.Text = waApiKey;
            if (txtDiscordToken != null) txtDiscordToken.Text = discToken;
            if (txtGuildId != null) txtGuildId.Text = guild;
            if (txtWebPin != null) txtWebPin.Text = pin;
            if (txtWebPort != null) txtWebPort.Text = port;

            string localIp = GetLocalIpAddress();
            if (lblWebUrlDisplay != null) {
                lblWebUrlDisplay.Text = "🌐 Canlı WiFi Web Paneli: http://" + localIp + ":" + currentWebPort;
            }
        }

        void SaveSettingsFromUI()
        {
            try {
                string cfgFile = @"C:\VakantieVeilingenBot\settings.json";
                var dict = new Dictionary<string, string>();
                dict["whatsapp_enabled"] = chkWhatsAppEnabled.Checked ? "1" : "0";
                dict["whatsapp_target_phone"] = txtWhatsAppPhone.Text.Trim();
                dict["whatsapp_apikey"] = txtWhatsAppApiKey.Text.Trim();
                dict["discord_token"] = txtDiscordToken.Text.Trim();
                dict["guild_id"] = txtGuildId.Text.Trim();
                dict["web_pin"] = string.IsNullOrEmpty(txtWebPin.Text.Trim()) ? "1234" : txtWebPin.Text.Trim();
                dict["web_port"] = string.IsNullOrEmpty(txtWebPort.Text.Trim()) ? "5000" : txtWebPort.Text.Trim();
                dict["settings_version"] = DateTime.UtcNow.Ticks.ToString();

                currentWebPort = dict["web_port"];
                File.WriteAllText(cfgFile, json.Serialize(dict));

                lblSettingsStatus.Text = "✅ Ayarlar başarıyla kaydedildi ve tüm sistemlerle eşitlendi!";
                lblSettingsStatus.ForeColor = AccentEmerald;

                AppendTerminal("SYSTEM", "Ayarlar kaydedildi (Port: " + currentWebPort + ", WA: " + (chkWhatsAppEnabled.Checked ? "Aktif" : "Pasif") + ")", AccentEmerald);
            } catch (Exception ex) {
                lblSettingsStatus.Text = "Hata: " + ex.Message;
                lblSettingsStatus.ForeColor = AccentRed;
                AppendTerminal("ERROR", "Ayarları kaydetme hatası: " + ex.Message, AccentRed);
            }
        }

        void SendTestWhatsApp()
        {
            lblTestWaResult.Text = "Mesaj gönderiliyor...";
            lblTestWaResult.ForeColor = TextMuted;

            string phone = txtWhatsAppPhone.Text.Trim();
            string apikey = txtWhatsAppApiKey.Text.Trim();

            Task.Run(() => {
                try {
                    string testUrl = "http://127.0.0.1:58433/test_whatsapp?phone=" + Uri.EscapeDataString(phone) + "&apikey=" + Uri.EscapeDataString(apikey);
                    var client = new WebClient();
                    string resp = client.DownloadString(testUrl);
                    var resDict = json.Deserialize<Dictionary<string, object>>(resp);
                    bool ok = resDict.ContainsKey("ok") && Convert.ToBoolean(resDict["ok"]);
                    string msg = resDict.ContainsKey("message") ? resDict["message"].ToString() : "";

                    BeginInvoke(new Action(() => {
                        lblTestWaResult.Text = (ok ? "✅ " : "❌ ") + msg;
                        lblTestWaResult.ForeColor = ok ? AccentEmerald : AccentRed;
                        AppendTerminal("WHATSAPP", "Test sonucu: " + msg, ok ? AccentEmerald : AccentRed);
                    }));
                } catch (Exception ex) {
                    BeginInvoke(new Action(() => {
                        lblTestWaResult.Text = "❌ İletişim Hatası: " + ex.Message;
                        lblTestWaResult.ForeColor = AccentRed;
                        AppendTerminal("ERROR", "WhatsApp iletişim hatası: " + ex.Message, AccentRed);
                    }));
                }
            });
        }

        void ToggleMax()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        void EnableDrag(Control c)
        {
            c.MouseDown += delegate(object s, MouseEventArgs e) {
                if (e.Button != MouseButtons.Left) return;
                if (e.Clicks == 2) { ToggleMax(); return; }
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
            };
        }

        async void OnLoaded(object sender, EventArgs e)
        {
            try {
                Directory.CreateDirectory(profilePath);
                Directory.CreateDirectory(screenshotsPath);

                adapterScript = File.Exists("adapter.js") ? File.ReadAllText("adapter.js") : "";
                darkCss = File.Exists("site-dark.css") ? File.ReadAllText("site-dark.css") : "";

                // Pure Chromium Force Dark Mode Options
                CoreWebView2EnvironmentOptions opts = null;
                try {
                    opts = new CoreWebView2EnvironmentOptions("--force-dark-mode", null, null, false);
                } catch { }

                environment = await CoreWebView2Environment.CreateAsync(null, profilePath, opts);

                // --- 1. Topbar WebView Init ---
                await topbarBrowser.EnsureCoreWebView2Async(environment);
                topbarBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                topbarBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                topbarBrowser.CoreWebView2.Settings.IsZoomControlEnabled = false;
                topbarBrowser.CoreWebView2.WebMessageReceived += OnTopbarWebMessage;
                topbarBrowser.CoreWebView2.NavigationCompleted += delegate { SyncTabsToTopbar(); SyncChromeLayout(); };
                string topbarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "topbar-ui.html");
                if (File.Exists(topbarPath)) topbarBrowser.Source = new Uri(topbarPath);

                // --- 2. Sidebar WebView Init ---
                await sidebarBrowser.EnsureCoreWebView2Async(environment);
                sidebarBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                sidebarBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                sidebarBrowser.CoreWebView2.Settings.IsZoomControlEnabled = false;
                sidebarBrowser.CoreWebView2.WebMessageReceived += OnSidebarWebMessage;
                sidebarBrowser.CoreWebView2.NavigationCompleted += delegate {
                    SyncChromeLayout();
                    SyncSidebarHud();
                    AppendTerminal("INFO", "Umutistan Luxury Sniper v2.0 devrede.", AccentEmerald);
                    AppendTerminal("SNIPER", "Varsayılan: Son 1 sn | Maks: 32 EUR | Artış: +1 EUR", AccentAmber);
                };
                string sidebarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sidebar-ui.html");
                if (File.Exists(sidebarPath)) sidebarBrowser.Source = new Uri(sidebarPath);

                // --- 3. Settings WebView Init ---
                await settingsBrowser.EnsureCoreWebView2Async(environment);
                settingsBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                settingsBrowser.CoreWebView2.Settings.IsZoomControlEnabled = false;
                settingsBrowser.CoreWebView2.WebMessageReceived += OnSettingsWebMessage;
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings-ui.html");
                if (File.Exists(settingsPath)) settingsBrowser.Source = new Uri(settingsPath);

                await terminalBrowser.EnsureCoreWebView2Async(environment);
                terminalBrowser.CoreWebView2.Settings.IsZoomControlEnabled = false;
                terminalBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                terminalBrowser.CoreWebView2.WebMessageReceived += OnWorkspaceMessage;
                terminalBrowser.CoreWebView2.NavigationCompleted += delegate {
                    terminalBrowser.CoreWebView2.ExecuteScriptAsync("if(window.loadEntries)window.loadEntries(" + json.Serialize(logHistory) + ");");
                };
                terminalBrowser.Source = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "terminal-ui.html"));
                await dialogBrowser.EnsureCoreWebView2Async(environment);
                dialogBrowser.CoreWebView2.Settings.IsZoomControlEnabled = false;
                dialogBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                dialogBrowser.CoreWebView2.WebMessageReceived += OnDialogMessage;
                dialogBrowser.Source = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dialog-ui.html"));
                foreach (var browser in new[] { topbarBrowser, sidebarBrowser, settingsBrowser, terminalBrowser }) {
                    string shortcuts = "if(!window.__umutKeys){window.__umutKeys=true;document.addEventListener('keydown',function(e){if(e.key==='F2'){e.preventDefault();chrome.webview.postMessage({action:'rename_tab'});}},true);}";
                    await browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(shortcuts);
                    await browser.CoreWebView2.ExecuteScriptAsync(shortcuts);
                }
                settingsBrowser.CoreWebView2.WebMessageReceived += OnWorkspaceMessage;

                await AddPage(uiTestMode ? "about:blank" : DefaultHome, null);
                if (uiTestMode) return;
                pollTimer.Start();

                EnsureBotServiceRunning();
                NotifyExternalEvent("SYSTEM", "Masaüstü Arayüzü Başlatıldı", 0, null, "Umutistan Sniper masaüstü kullanıcı arayüzü başarıyla açıldı.");
                AppendTerminal("SYSTEM", "Discord Bot 6/6 kanal senkronizasyonu devrede.", AccentEmerald);
            }
            catch (Exception ex) {
                AppendTerminal("ERROR", "Başlatma Hatası: " + ex.Message, AccentRed);
            }
        }

        async Task AddPage(string rawUrl, SavedState saved)
        {
            if (environment == null || closing) return;

            string url = uiTestMode && rawUrl == "about:blank" ? rawUrl : NormalizeAuctionUrl(rawUrl);
            var page = new PageItem();
            if (saved != null) {
                page.Max = saved.Max; page.Step = saved.Step; page.Seconds = saved.Seconds;
            } else {
                page.Max = 32; page.Step = 1; page.Seconds = 1;
            }

            page.Browser = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
            webCard.Controls.Add(page.Browser);

            page.Url = url;
            pages.Add(page);
            SyncTabsToTopbar();

            await page.Browser.EnsureCoreWebView2Async(environment);
            ConfigureBrowser(page);
            page.Browser.Source = new Uri(url);
            SelectPage(page);
            NotifyExternalEvent("AUCTION", "İhale İzleniyor", page.Max, null, url);
            AppendTerminal("AUCTION", "Yeni sekme açıldı: " + Truncate(url, 45), AccentCyan);
        }

        void ConfigureBrowser(PageItem page)
        {
            var s = page.Browser.CoreWebView2.Settings;
            s.IsStatusBarEnabled = false;
            s.AreDefaultContextMenusEnabled = true;
            s.IsZoomControlEnabled = true;
            page.Browser.CoreWebView2.WebMessageReceived += OnWorkspaceMessage;
            page.Browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("document.addEventListener('keydown',function(e){if(e.key==='F2'){e.preventDefault();chrome.webview.postMessage({action:'rename_tab'});}else if(e.key==='F5'||((e.ctrlKey||e.metaKey)&&(e.key==='r'||e.key==='R'))){e.preventDefault();chrome.webview.postMessage({action:'nav_reload'});}else if(e.altKey&&e.key==='ArrowLeft'){e.preventDefault();chrome.webview.postMessage({action:'nav_back'});}else if(e.altKey&&e.key==='ArrowRight'){e.preventDefault();chrome.webview.postMessage({action:'nav_forward'});}},true);");
            s.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

            page.Browser.CoreWebView2.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs args) {
                if (uiTestMode) return;
                try {
                    var uri = new Uri(args.Uri);
                    string host = uri.Host.ToLowerInvariant();
                    if (!host.Contains("vakantieveilingen.be") && 
                        !host.Contains("vakantieveilingen.nl") && 
                        !host.Contains("talpa") && 
                        !host.Contains("login") && 
                        !host.Contains("auth") && 
                        !host.Contains("identity") &&
                        !host.Contains("127.0.0.1") &&
                        !host.Contains("localhost") &&
                        !args.Uri.StartsWith("about:blank"))
                    {
                        args.Cancel = true;
                    }
                } catch { }
            };

            page.Browser.CoreWebView2.DOMContentLoaded += async delegate {
                try {
                    await InjectPageScripts(page);
                } catch { }
            };

            page.Browser.CoreWebView2.NavigationCompleted += async delegate(object sender, CoreWebView2NavigationCompletedEventArgs args) {
                page.Ready = args.IsSuccess;
                if (!args.IsSuccess) AppendTerminal("ERROR", "Sayfa yüklenemedi: " + args.WebErrorStatus + " — " + page.Url, AccentRed);
                await InjectPageScripts(page);
                SyncTabsToTopbar();
            };

            page.Browser.CoreWebView2.SourceChanged += delegate {
                page.Url = page.Browser.Source != null ? page.Browser.Source.ToString() : "";
                if (page == current && sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                    sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.updateCurrentUrl) window.updateCurrentUrl({0});", json.Serialize(page.Url)));
                }
            };
        }

        async Task InjectPageScripts(PageItem page)
        {
            if (page == null || page.Browser == null || page.Browser.CoreWebView2 == null) return;
            try {
                Uri source;
                bool auctionSite = Uri.TryCreate(page.Url, UriKind.Absolute, out source) && (source.Host == "www.vakantieveilingen.be" || source.Host == "www.vakantieveilingen.nl" || source.Host == "vakantieveilingen.be" || source.Host == "vakantieveilingen.nl");
                if (!auctionSite) return;
                if (!string.IsNullOrEmpty(darkCss)) {
                    string cssInject = "(() => { let el=document.getElementById('umutistan-dark');if(!el){el=document.createElement('style');el.id='umutistan-dark';document.head.appendChild(el);}el.textContent=" + json.Serialize(darkCss) + "; })();";
                    await page.Browser.CoreWebView2.ExecuteScriptAsync(cssInject);
                    string themeScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "site-theme.js");
                    if (File.Exists(themeScript)) await page.Browser.CoreWebView2.ExecuteScriptAsync(File.ReadAllText(themeScript));
                }
                if (!string.IsNullOrEmpty(adapterScript)) {
                    await page.Browser.CoreWebView2.ExecuteScriptAsync(adapterScript);
                }
            } catch { }
        }

        void SelectPage(PageItem page)
        {
            if (page == null || !pages.Contains(page)) return;
            isSettingsActive = false;
            isTerminalActive = false;
            terminalView.Visible = false;

            pnlSettingsView.Visible = false;
            webCard.Visible = true;

            current = page;
            foreach (var p in pages) {
                p.Browser.Visible = (p == page);
            }
            syncing = true;
            numMax.Value = page.Max;
            numStep.Value = page.Step;
            numSeconds.Value = page.Seconds;
            syncing = false;

            SyncTabsToTopbar();
            SyncSidebarHud();
        }

        void ClosePage(PageItem page)
        {
            if (pages.Count <= 1) return;
            if (page.Pinned) { ShowTabEditor(page); return; }
            if (page.Armed) { ShowDialog("close-tab", page); return; }
            RemovePage(page);
        }

        void RemovePage(PageItem page)
        {
            page.Armed = false;
            pages.Remove(page);
            webCard.Controls.Remove(page.Browser);
            page.Browser.Dispose();
            if (current == page) SelectPage(pages.LastOrDefault());
            else SyncTabsToTopbar();
            AppendTerminal("AUCTION", "Sekme kapatıldı.", TextMuted);
        }

        void ToggleCurrentArm()
        {
            if (current == null) return;
            current.Armed = !current.Armed;
            SyncSidebarHud();
            SyncTabsToTopbar();
            AppendTerminal("SNIPER", (current.Armed ? "🟢 İZLEME BAŞLATILDI: " : "⏸️ İZLEME DURDURULDU: ") + Truncate(current.Title, 25), current.Armed ? AccentEmerald : AccentAmber);
        }

        void UpdateArmButton(PageItem page)
        {
            SyncSidebarHud();
            SyncTabsToTopbar();
        }

        void SyncTabsToTopbar()
        {
            if (topbarBrowser == null || topbarBrowser.CoreWebView2 == null) return;
            try {
                var list = new List<object>();
                for (int i = 0; i < pages.Count; i++) {
                    var p = pages[i];
                    list.Add(new {
                        index = i,
                        title = !string.IsNullOrEmpty(p.CustomTitle) ? p.CustomTitle : (string.IsNullOrEmpty(p.Title) ? ("İhale #" + (i + 1)) : p.Title),
                        active = (p == current && !isSettingsActive && !isTerminalActive),
                        pinned = p.Pinned,
                        armed = p.Armed
                    });
                }
                string jsonArray = json.Serialize(list);
                string script = string.Format("if (window.renderTabs) window.renderTabs({0}, {1});", jsonArray, isSettingsActive.ToString().ToLower());
                topbarBrowser.CoreWebView2.ExecuteScriptAsync(script);
            } catch { }
        }

        void SyncSidebarHud()
        {
            if (sidebarBrowser == null || sidebarBrowser.CoreWebView2 == null || current == null) return;
            try {
                string timerText = "00 : 00 : 01";
                int secondsLeft = 1;
                if (current.State != null && current.State.IsAuction) {
                    timerText = current.State.TimerText ?? (current.State.Seconds >= 0 ? current.State.Seconds + " sn" : "00 : 00 : 01");
                    secondsLeft = current.State.Seconds;
                }
                bool isArmed = current.Armed;
                decimal maxVal = current.Max;
                decimal stepVal = current.Step;
                int secVal = current.Seconds;

                string script = string.Format("if (window.updateHud) window.updateHud('{0}', {1}, {2}, {3}, {4}, {5});",
                    timerText,
                    isArmed.ToString().ToLower(),
                    maxVal.ToString(CultureInfo.InvariantCulture),
                    stepVal.ToString(CultureInfo.InvariantCulture),
                    secVal,
                    secondsLeft);
                sidebarBrowser.CoreWebView2.ExecuteScriptAsync(script);
                if (current != null && !string.IsNullOrEmpty(current.Url)) {
                    sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if (window.updateCurrentUrl) window.updateCurrentUrl({0});", json.Serialize(current.Url)));
                }
            } catch { }
        }

        void OnTopbarWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try {
                string raw = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(raw)) {
                    try { raw = e.TryGetWebMessageAsString(); } catch { }
                }
                var msg = json.Deserialize<Dictionary<string, object>>(raw);
                if (msg == null || !msg.ContainsKey("action")) return;
                string action = msg["action"].ToString();

                if (action == "home" || action == "rename_tab" || action == "open_terminal") { HandleWorkspaceAction(action); }
                else if (action == "start_drag") {
                    BeginInvoke(new Action(() => {
                        Point point = Cursor.Position;
                        DateTime now = DateTime.UtcNow;
                        Size tolerance = SystemInformation.DoubleClickSize;
                        if ((now - lastCaptionClick).TotalMilliseconds <= SystemInformation.DoubleClickTime &&
                            Math.Abs(point.X - lastCaptionPoint.X) <= tolerance.Width && Math.Abs(point.Y - lastCaptionPoint.Y) <= tolerance.Height) {
                            lastCaptionClick = DateTime.MinValue;
                            ToggleMax();
                            return;
                        }
                        lastCaptionClick = now;
                        lastCaptionPoint = point;
                        ReleaseCapture();
                        SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
                    }));
                }
                else if (action == "minimize") {
                    BeginInvoke(new Action(() => WindowState = FormWindowState.Minimized));
                }
                else if (action == "maximize") {
                    BeginInvoke(new Action(() => ToggleMax()));
                }
                else if (action == "close") {
                    BeginInvoke(new Action(() => Close()));
                }
                else if (action == "add_tab") {
                    BeginInvoke(new Action(async () => await AddPage(DefaultHome, null)));
                }
                else if (action == "open_web_panel") {
                    BeginInvoke(new Action(async () => await AddPage("http://127.0.0.1:" + currentWebPort, null)));
                }
                else if (action == "open_settings") {
                    BeginInvoke(new Action(() => SelectSettingsTab()));
                }
                else if (action == "select_tab") {
                    if (msg.ContainsKey("index")) {
                        int idx = Convert.ToInt32(msg["index"]);
                        if (idx >= 0 && idx < pages.Count) {
                            BeginInvoke(new Action(() => SelectPage(pages[idx])));
                        }
                    }
                }
                else if (action == "close_tab") {
                    if (msg.ContainsKey("index")) {
                        int idx = Convert.ToInt32(msg["index"]);
                        if (idx >= 0 && idx < pages.Count) {
                            BeginInvoke(new Action(() => ClosePage(pages[idx])));
                        }
                    }
                }
            } catch { }
        }

        void OnSidebarWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try {
                string raw = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(raw)) {
                    try { raw = e.TryGetWebMessageAsString(); } catch { }
                }
                var msg = json.Deserialize<Dictionary<string, object>>(raw);
                if (msg == null || !msg.ContainsKey("action")) return;
                string action = msg["action"].ToString();

                if (action == "toggle_controls") {
                    controlsExpanded = !controlsExpanded;
                    ApplyResponsiveLayout();
                }
                else if (action == "open_terminal" || action == "home" || action == "rename_tab" || action == "show_auctions") { HandleWorkspaceAction(action); }
                else if (action == "toggle_arm") {
                    BeginInvoke(new Action(() => ToggleCurrentArm()));
                }
                else if (action == "update_params") {
                    if (current != null) {
                        if (msg.ContainsKey("max")) current.Max = Convert.ToDecimal(msg["max"]);
                        if (msg.ContainsKey("step")) current.Step = Convert.ToDecimal(msg["step"]);
                        if (msg.ContainsKey("sec")) current.Seconds = Convert.ToInt32(msg["sec"]);
                        numMax.Value = current.Max;
                        numStep.Value = current.Step;
                        numSeconds.Value = current.Seconds;
                        AppendTerminal("SETTINGS", "Parametreler güncellendi: Limit " + current.Max + " €, Artış +" + current.Step + " €, Tetik " + current.Seconds + "s", AccentCyan);
                    }
                }
                else if (action == "update_checkbox") {
                    if (msg.ContainsKey("key") && msg["key"].ToString() == "stop_on_win") {
                        bool val = Convert.ToBoolean(msg["value"]);
                        chkStopOnWin.Checked = val;
                    }
                }
                else if (action == "copy_url") {
                    BeginInvoke(new Action(() => CopyCurrentAuctionUrl()));
                }
                else if (action == "copy_mobile") {
                    BeginInvoke(new Action(() => CopyMobileLink()));
                }
                else if (action == "navigate_url") {
                    string targetUrl = msg.ContainsKey("url") ? msg["url"].ToString() : "";
                    BeginInvoke(new Action(() => {
                        string norm;
                        string err;
                        if (ValidateAndNormalizeAuctionUrl(targetUrl, out norm, out err)) {
                            if (current != null && current.Browser != null && current.Browser.CoreWebView2 != null) {
                                current.Url = norm;
                                current.Browser.CoreWebView2.Navigate(norm);
                                AppendTerminal("AUCTION", "İhale URL'sine yönlendirildi: " + norm, AccentEmerald);
                                if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                                    sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.showUrlFeedback) window.showUrlFeedback(true, {0}, {1});", json.Serialize("✓ Yönlendirildi"), json.Serialize(norm)));
                                }
                            }
                        } else {
                            AppendTerminal("ERROR", "Geçersiz İhale URL'si: " + err, AccentRed);
                            if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                                sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.showUrlFeedback) window.showUrlFeedback(false, {0}, '');", json.Serialize(err)));
                            }
                        }
                    }));
                }
                else if (action == "new_tab_url") {
                    string targetUrl = msg.ContainsKey("url") ? msg["url"].ToString() : "";
                    BeginInvoke(new Action(async () => {
                        string norm;
                        string err;
                        if (ValidateAndNormalizeAuctionUrl(targetUrl, out norm, out err)) {
                            await AddPage(norm, null);
                            if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                                await sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.showUrlFeedback) window.showUrlFeedback(true, {0}, {1});", json.Serialize("✓ Yeni sekmede açıldı"), json.Serialize(norm)));
                            }
                        } else {
                            AppendTerminal("ERROR", "Geçersiz İhale URL'si: " + err, AccentRed);
                            if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                                await sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.showUrlFeedback) window.showUrlFeedback(false, {0}, '');", json.Serialize(err)));
                            }
                        }
                    }));
                }
                else if (action == "paste_url") {
                    BeginInvoke(new Action(() => {
                        try {
                            string clip = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(clip) && sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                                sidebarBrowser.CoreWebView2.ExecuteScriptAsync(string.Format("if(window.setAuctionUrlInput) window.setAuctionUrlInput({0});", json.Serialize(clip.Trim())));
                            }
                        } catch { }
                    }));
                }
                else if (action == "nav_reload") {
                    BeginInvoke(new Action(() => ReloadCurrentTab()));
                }
                else if (action == "nav_back") {
                    BeginInvoke(new Action(() => NavigateBack()));
                }
                else if (action == "nav_forward") {
                    BeginInvoke(new Action(() => NavigateForward()));
                }
                else if (action == "set_shutdown_mode") {
                    if (msg.ContainsKey("mode")) {
                        shutdownMode = msg["mode"].ToString();
                        AppendTerminal("SYSTEM", "PC Otomatik Kapatma modu güncellendi: " + shutdownMode, AccentCyan);
                    }
                }
                else if (action == "abort_shutdown") {
                    BeginInvoke(new Action(() => AbortPcShutdown()));
                }
            } catch { }
        }

        void OnSettingsWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try {
                string raw = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(raw)) {
                    try { raw = e.TryGetWebMessageAsString(); } catch { }
                }
                var msg = json.Deserialize<Dictionary<string, object>>(raw);
                if (msg == null || !msg.ContainsKey("action")) return;
                string action = msg["action"].ToString();

                if (action == "close_settings") {
                    BeginInvoke(new Action(() => {
                        if (pages.Count > 0) SelectPage(current ?? pages[0]);
                        else { var t = AddPage(DefaultHome, null); }
                    }));
                }
                else if (action == "save_settings") {
                    if (msg.ContainsKey("data")) {
                        var data = (Dictionary<string, object>)msg["data"];
                        SaveSettingsFromWeb(data);
                    }
                }
                else if (action == "test_whatsapp") {
                    string phone = msg.ContainsKey("phone") ? msg["phone"].ToString() : "";
                    string apikey = msg.ContainsKey("apikey") ? msg["apikey"].ToString() : "";
                    SendTestWhatsAppFromWeb(phone, apikey);
                }
                else if (action == "copy_web_url") {
                    string url = msg.ContainsKey("url") ? msg["url"].ToString() : "";
                    if (!string.IsNullOrEmpty(url)) {
                        BeginInvoke(new Action(() => {
                            try { Clipboard.SetText(url); } catch { }
                        }));
                    }
                }
            } catch { }
        }

        void SaveSettingsFromWeb(Dictionary<string, object> data)
        {
            try {
                string cfgFile = @"C:\VakantieVeilingenBot\settings.json";
                var dict = new Dictionary<string, string>();
                foreach (var kv in data) {
                    dict[kv.Key] = kv.Value != null ? kv.Value.ToString() : "";
                }
                dict["settings_version"] = DateTime.UtcNow.Ticks.ToString();
                if (dict.ContainsKey("web_port")) currentWebPort = dict["web_port"];

                File.WriteAllText(cfgFile, json.Serialize(dict));

                if (settingsBrowser != null && settingsBrowser.CoreWebView2 != null) {
                    settingsBrowser.CoreWebView2.ExecuteScriptAsync("if (window.onSettingsSaved) window.onSettingsSaved(true, 'Ayarlar başarıyla kaydedildi ve tüm sistemlerle eşitlendi!');");
                }
                AppendTerminal("SYSTEM", "Ayarlar kaydedildi (Port: " + currentWebPort + ", WA: " + (dict.ContainsKey("whatsapp_enabled") && dict["whatsapp_enabled"] == "1" ? "Aktif" : "Pasif") + ")", AccentEmerald);
            } catch (Exception ex) {
                if (settingsBrowser != null && settingsBrowser.CoreWebView2 != null) {
                    string errScript = string.Format("if (window.onSettingsSaved) window.onSettingsSaved(false, '{0}');", EscapeJsString(ex.Message));
                    settingsBrowser.CoreWebView2.ExecuteScriptAsync(errScript);
                }
                AppendTerminal("ERROR", "Ayarları kaydetme hatası: " + ex.Message, AccentRed);
            }
        }

        void SendTestWhatsAppFromWeb(string phone, string apikey)
        {
            Task.Run(() => {
                try {
                    string testUrl = "http://127.0.0.1:58433/test_whatsapp?phone=" + Uri.EscapeDataString(phone) + "&apikey=" + Uri.EscapeDataString(apikey);
                    var client = new WebClient();
                    string resp = client.DownloadString(testUrl);
                    var resDict = json.Deserialize<Dictionary<string, object>>(resp);
                    bool ok = resDict.ContainsKey("ok") && Convert.ToBoolean(resDict["ok"]);
                    string msg = resDict.ContainsKey("message") ? resDict["message"].ToString() : "";

                    BeginInvoke(new Action(() => {
                        if (settingsBrowser != null && settingsBrowser.CoreWebView2 != null) {
                            string resScript = string.Format("if (window.onWhatsAppTestResult) window.onWhatsAppTestResult({0}, '{1}');", ok.ToString().ToLower(), EscapeJsString(msg));
                            settingsBrowser.CoreWebView2.ExecuteScriptAsync(resScript);
                        }
                        AppendTerminal("WHATSAPP", "Test sonucu: " + msg, ok ? AccentEmerald : AccentRed);
                    }));
                } catch (Exception ex) {
                    BeginInvoke(new Action(() => {
                        if (settingsBrowser != null && settingsBrowser.CoreWebView2 != null) {
                            string errScript = string.Format("if (window.onWhatsAppTestResult) window.onWhatsAppTestResult(false, '{0}');", EscapeJsString(ex.Message));
                            settingsBrowser.CoreWebView2.ExecuteScriptAsync(errScript);
                        }
                        AppendTerminal("ERROR", "WhatsApp iletişim hatası: " + ex.Message, AccentRed);
                    }));
                }
            });
        }

        static string EscapeJsString(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "\\n");
        }

        void SettingChanged(object sender, EventArgs e)
        {
            if (syncing || current == null) return;
            current.Max = numMax.Value;
            current.Step = numStep.Value;
            current.Seconds = (int)numSeconds.Value;
            AppendTerminal("SETTINGS", "Parametreler güncellendi: Limit " + current.Max + " €, Artış +" + current.Step + " €, Süre " + current.Seconds + "s", AccentCyan);
        }

        async void PollPages(object sender, EventArgs e)
        {
            if (busy || closing) return;
            busy = true;
            try {
                for (int i = 0; i < pages.Count; i++) {
                    var p = pages[i];
                    if (p.Ready && p.Browser != null && p.Browser.CoreWebView2 != null) {
                        await ProcessPage(p);
                    }
                }
            } catch { }
            finally { busy = false; }
        }

        async Task ProcessPage(PageItem page)
        {
            try {
                // Adapter var mı kontrol et, yoksa anında enjekte et
                string check = await page.Browser.CoreWebView2.ExecuteScriptAsync("typeof window.__bieder");
                if (check != "\"object\"") {
                    await InjectPageScripts(page);
                }

                string script = "window.__bieder ? JSON.stringify(window.__bieder.getState()) : ''";
                string jsonStr = await page.Browser.CoreWebView2.ExecuteScriptAsync(script);
                if (string.IsNullOrEmpty(jsonStr) || jsonStr == "null" || jsonStr == "\"\"") return;

                string unescaped = json.Deserialize<string>(jsonStr);
                if (string.IsNullOrEmpty(unescaped)) return;

                var snap = json.Deserialize<Snapshot>(unescaped);
                if (snap == null) return;
                page.State = snap;
                if (!string.IsNullOrEmpty(snap.Title) && snap.Title != page.Title) {
                    page.Title = snap.Title;
                    SyncTabsToTopbar();
                }

                if (page == current && !isSettingsActive) UpdateTelemetry(page);

                // Otomatik Teklif & Sniper Mantığı (Son 1 saniye kuralı & Limit Aşımı Kontrolü)
                if (page.Armed && snap.IsAuction && !snap.IsWinner && !snap.IsLost) {
                    decimal nextTarget = snap.MinimumBid > 0 ? snap.MinimumBid : (snap.CurrentBid + page.Step);
                    bool limitExceeded = snap.CurrentBid >= page.Max || nextTarget > page.Max;

                    if (limitExceeded && !snap.OwnHighest) {
                        // Limit aşıldı! Discord ve WhatsApp bildirimi ilet
                        if (!page.LimitWarned || snap.CurrentBid > page.LastWarnedBid) {
                            page.LimitWarned = true;
                            page.LastWarnedBid = snap.CurrentBid;
                            string warnMsg = string.Format("⚠️ LİMİT AŞILDI: '{0}' ihalesinde güncel teklif ({1} €), belirlediğiniz limiti ({2} €) aştı! Limiti yükseltmezseniz bot durdurulacaktır.", Truncate(page.Title, 35), snap.CurrentBid, page.Max);
                            NotifyExternalEvent("ALERT", page.Title, snap.CurrentBid, null, warnMsg);
                            SendDirectWhatsApp(warnMsg);
                            AppendTerminal("LIMIT", string.Format("⚠️ LİMİT AŞIMI: İhale {0} € oldu (Limit: {1} €). Limiti yükseltmezseniz bot durdurulacak.", snap.CurrentBid, page.Max), AccentAmber);
                        }

                        // Tetikleme süresi geldiyse (veya ihale bittiyse) ve limit hala yükseltilmediyse botu durdur
                        bool timeTriggerOrEnd = (snap.Seconds >= 0 && snap.Seconds <= page.Seconds);
                        if (timeTriggerOrEnd) {
                            page.Armed = false;
                            page.LimitWarned = false;
                            UpdateArmButton(page);
                            UpdateTabUI(page);
                            string disarmMsg = string.Format("🛑 LİMİT YÜKSELTİLMEDİĞİ İÇİN BOT DURDURULDU! '{0}' ihalesinde son teklif {1} € oldu (Limit: {2} €).", Truncate(page.Title, 35), snap.CurrentBid, page.Max);
                            NotifyExternalEvent("ALERT", page.Title, snap.CurrentBid, null, disarmMsg);
                            SendDirectWhatsApp(disarmMsg);
                            AppendTerminal("DISARM", string.Format("🛑 LİMİT AŞIMI: Limit artırılmadığı için bot durduruldu ({0} € > {1} €).", snap.CurrentBid, page.Max), AccentRed);

                            if (shutdownMode == "on_limit" || shutdownMode == "on_any") {
                                TriggerPcShutdown(string.Format("İhale limiti aşıldı ({0} € > {1} €)", snap.CurrentBid, page.Max));
                            }
                        }
                    } else if (page.LimitWarned && page.Max > snap.CurrentBid && nextTarget <= page.Max) {
                        // Limit kullanıcı veya bot tarafından yükseltildi
                        page.LimitWarned = false;
                        AppendTerminal("SETTINGS", string.Format("Limit yükseltildi ({0} €), izleme aktif devam ediyor.", page.Max), AccentEmerald);
                    }

                    // Normal Sniper Tetiklemesi (Limit aşılmamışsa ve kendi teklifimiz en yüksek değilse)
                    if (page.Armed && !snap.OwnHighest && !limitExceeded) {
                        decimal target = nextTarget;
                        if (target > page.Max) target = page.Max;

                        bool timeTrigger = (snap.Seconds >= 0 && snap.Seconds <= page.Seconds);
                        if (timeTrigger && target <= page.Max && page.Pending != target) {
                            page.Pending = target;
                            NotifyExternalEvent("ALERT", page.Title, snap.CurrentBid, null, "🚨 Son " + snap.Seconds + " saniye! Sniper teklifi (" + target + " €) iletiliyor...");
                            AppendTerminal("SNIPER", "TETİKLEME: Son " + snap.Seconds + " sn! Teklif: " + target + " €", AccentAmber);
                            await ExecuteBid(page, target);
                        }
                    }
                }

                // Kazanç durumu
                if (snap.IsWinner && page.Armed) {
                    page.Armed = false;
                    UpdateArmButton(page);
                    UpdateTabUI(page);

                    string snapPath = null;
                    try {
                        snapPath = Path.Combine(screenshotsPath, "win_" + DateTime.Now.Ticks + ".png");
                        using (var fs = File.Create(snapPath)) {
                            await page.Browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
                        }
                    } catch { }

                    NotifyExternalEvent("WIN", page.Title, snap.CurrentBid, snapPath, "Tebrikler! İhale kazanıldı.");
                    AppendTerminal("WIN", "🏆 TEBRİKLER! İhale kazanıldı: " + snap.CurrentBid + " €", AccentEmerald);

                    if (chkStopOnWin.Checked) {
                        foreach (var other in pages) other.Armed = false;
                        UpdateArmButton(current);
                    }

                    if (shutdownMode == "on_end" || shutdownMode == "on_any") {
                        TriggerPcShutdown(string.Format("İhale kazanıldı ({0} €)", snap.CurrentBid));
                    }
                }
                else if (snap.IsLost && page.Armed) {
                    page.Armed = false;
                    UpdateArmButton(page);
                    UpdateTabUI(page);
                    AppendTerminal("AUCTION", "İhale sonlandı: " + Truncate(page.Title, 25), AccentAmber);
                    if (shutdownMode == "on_end" || shutdownMode == "on_any") {
                        TriggerPcShutdown("İhale sonlandı");
                    }
                }
            } catch { }
        }

        async Task ExecuteBid(PageItem page, decimal amount)
        {
            try {
                string bidScript = "if (window.__bieder) { window.__bieder.placeBid(" + amount.ToString(CultureInfo.InvariantCulture) + "); }";
                await page.Browser.CoreWebView2.ExecuteScriptAsync(bidScript);
                NotifyExternalEvent("BID", page.Title, amount, null, "Otomatik sniper teklifi verildi: " + amount + " €");
                AppendTerminal("BID", "Teklif başarıyla gönderildi: " + amount + " €", AccentEmerald);
            } catch (Exception ex) {
                NotifyExternalEvent("ERROR", page.Title, amount, null, "Teklif iletme hatası: " + ex.Message);
                AppendTerminal("ERROR", "Teklif gönderme hatası: " + ex.Message, AccentRed);
            }
        }

        void UpdateTabUI(PageItem page)
        {
            SyncTabsToTopbar();
        }

        void UpdateTelemetry(PageItem page)
        {
            if (page == null) return;
            SyncSidebarHud();
        }

        void SetupIpcServer()
        {
            Task.Run(() => {
                try {
                    ipcListener = new HttpListener();
                    ipcListener.Prefixes.Add("http://127.0.0.1:58432/");
                    ipcListener.Start();

                    while (!closing) {
                        var ctx = ipcListener.GetContext();
                        Task.Run(() => HandleIpcRequest(ctx));
                    }
                } catch { }
            });
        }

        void HandleIpcRequest(HttpListenerContext ctx)
        {
            try {
                var req = ctx.Request;
                var res = ctx.Response;
                res.ContentType = "application/json; charset=utf-8";

                string path = req.Url.AbsolutePath.ToLowerInvariant();
                if (path == "/status") {
                    var list = pages.Where(p => p.Ready).Select(p => new {
                        title = p.Title,
                        url = p.Url,
                        armed = p.Armed,
                        max = p.Max,
                        step = p.Step,
                        seconds = p.Seconds,
                        currentBid = p.State != null ? p.State.CurrentBid : 0,
                        timerText = p.State != null ? p.State.TimerText : "—",
                        isWinner = p.State != null && p.State.IsWinner
                    }).ToList();

                    byte[] b = Encoding.UTF8.GetBytes(json.Serialize(list));
                    res.OutputStream.Write(b, 0, b.Length);
                }
                else if (path == "/stop") {
                    string url = req.QueryString["url"];
                    Invoke(new Action(() => {
                        var target = pages.FirstOrDefault(p => p.Url == url);
                        if (target != null) {
                            target.Armed = false;
                            UpdateArmButton(target);
                            UpdateTabUI(target);
                        } else {
                            foreach (var p in pages) p.Armed = false;
                            UpdateArmButton(current);
                        }
                    }));
                    byte[] b = Encoding.UTF8.GetBytes("{\"ok\":true}");
                    res.OutputStream.Write(b, 0, b.Length);
                }
                else if (path == "/add") {
                    string rawUrl = req.QueryString["url"];
                    string url = NormalizeAuctionUrl(rawUrl);
                    decimal m, s;
                    int sc;
                    decimal max = decimal.TryParse(req.QueryString["max"], out m) ? m : 32;
                    decimal step = decimal.TryParse(req.QueryString["step"], out s) ? s : 1;
                    int sec = int.TryParse(req.QueryString["sec"], out sc) ? sc : 1;

                    if (!string.IsNullOrEmpty(url)) {
                        BeginInvoke(new Action(async () => {
                            await AddPage(url, new SavedState { Max = max, Step = step, Seconds = sec });
                        }));
                    }
                    byte[] b = Encoding.UTF8.GetBytes("{\"ok\":true}");
                    res.OutputStream.Write(b, 0, b.Length);
                }
                else if (path == "/screenshot") {
                    string outPath = Path.Combine(screenshotsPath, "snap_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    if (current != null && current.Ready) {
                        Invoke(new Action(async () => {
                            try {
                                using (var stream = new FileStream(outPath, FileMode.Create)) {
                                    await current.Browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                                }
                            } catch { }
                        }));
                    }
                    byte[] b = Encoding.UTF8.GetBytes(json.Serialize(new { ok = true, path = outPath }));
                    res.OutputStream.Write(b, 0, b.Length);
                }
                else if (path == "/set_limit" || path == "/update") {
                    string url = req.QueryString["url"];
                    decimal m;
                    string rawMax = req.QueryString["max"] ?? req.QueryString["limit"];
                    if (!string.IsNullOrEmpty(rawMax) && decimal.TryParse(rawMax, NumberStyles.Any, CultureInfo.InvariantCulture, out m)) {
                        Invoke(new Action(() => {
                            var target = !string.IsNullOrEmpty(url) ? pages.FirstOrDefault(p => p.Url == url) : current;
                            if (target != null) {
                                target.Max = m;
                                target.LimitWarned = false;
                                if (target == current) {
                                    numMax.Value = Math.Min(numMax.Maximum, Math.Max(numMax.Minimum, m));
                                    SyncSidebarHud();
                                }
                                AppendTerminal("SETTINGS", string.Format("İhale limiti güncellendi: {0} € ({1})", m, Truncate(target.Title, 25)), AccentCyan);
                            }
                        }));
                    }
                    byte[] b = Encoding.UTF8.GetBytes("{\"ok\":true}");
                    res.OutputStream.Write(b, 0, b.Length);
                }
                else if (path == "/shutdown") {
                    string cmd = req.QueryString["cmd"];
                    if (cmd == "abort") {
                        Invoke(new Action(() => AbortPcShutdown()));
                    } else {
                        string reason = req.QueryString["reason"] ?? "Uzak komut ile tetiklendi";
                        Invoke(new Action(() => TriggerPcShutdown(reason)));
                    }
                    byte[] b = Encoding.UTF8.GetBytes("{\"ok\":true}");
                    res.OutputStream.Write(b, 0, b.Length);
                }
                res.Close();
            } catch { }
        }

        void EnsureBotServiceRunning()
        {
            try {
                if (Process.GetProcessesByName("BotService").Length == 0) {
                    string installDir = @"C:\VakantieVeilingenBot";
                    string botExe = Path.Combine(installDir, "BotService.exe");
                    if (File.Exists(botExe)) {
                        Process.Start(new ProcessStartInfo(botExe) {
                            WorkingDirectory = installDir,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        AppendTerminal("SYSTEM", "BotService arka planda başlatıldı.", AccentCyan);
                    }
                }
            } catch { }
        }

        void NotifyExternalEvent(string type, string title, decimal price, string image = null, string details = null)
        {
            Color c = AccentEmerald;
            if (type == "ERROR") c = AccentRed;
            else if (type == "ALERT") c = AccentAmber;
            else if (type == "AUCTION") c = AccentCyan;
            else if (type == "SYSTEM") c = AccentBlue;
            AppendTerminal(type, title + (details != null ? ": " + details : ""), c);

            if (uiTestMode) return;

            Task.Run(() => {
                try {
                    var client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=utf-8";
                    var payload = json.Serialize(new { type = type, title = title, price = price, image = image, details = details });
                    client.UploadString("http://127.0.0.1:58433/event", payload);
                } catch { }
            });
        }

        void SendDirectWhatsApp(string message)
        {
            if (uiTestMode) return;
            Task.Run(() => {
                try {
                    string phone = txtWhatsAppPhone != null ? txtWhatsAppPhone.Text.Trim() : "";
                    string apikey = txtWhatsAppApiKey != null ? txtWhatsAppApiKey.Text.Trim() : "";
                    bool enabled = chkWhatsAppEnabled != null && chkWhatsAppEnabled.Checked;
                    if (!enabled || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(apikey)) return;

                    string clean = phone.Replace(" ", "").Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "");
                    string url = "https://api.callmebot.com/whatsapp.php?phone=" + Uri.EscapeDataString(clean) + "&text=" + Uri.EscapeDataString(message) + "&apikey=" + Uri.EscapeDataString(apikey);
                    using (var wc = new WebClient()) {
                        wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                        wc.DownloadString(url);
                    }
                } catch { }
            });
        }

        void TriggerPcShutdown(string reason)
        {
            if (shutdownTriggered || uiTestMode) return;
            shutdownTriggered = true;
            try {
                AppendTerminal("SHUTDOWN", "⚠️ OTOMATİK KAPATMA: " + reason + " - Bilgisayar 60 saniye içinde kapatılacak! İptal etmek için menüdeki [⛔ İPTAL] butonuna tıklayın veya 'shutdown /a' yazın.", AccentRed);
                NotifyExternalEvent("ALERT", "Otomatik PC Kapatma", 0, null, "⚠️ Bilgisayar 60 saniye içinde kapatılıyor! Neden: " + reason);
                SendDirectWhatsApp("⚠️ BİLGİSAYAR KAPATILIYOR! Umutistan Bot: " + reason + " - 60 saniye içinde sistem kapatılacak. İptal için komut satırına 'shutdown /a' yazabilirsiniz.");

                var psi = new ProcessStartInfo("shutdown.exe", "/s /t 60 /c \"Umutistan Bot: " + reason + " - 60 sn icinde kapatiliyor...\"") {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);

                if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                    sidebarBrowser.CoreWebView2.ExecuteScriptAsync("if(window.onShutdownCountdown) window.onShutdownCountdown(60);");
                }
            } catch (Exception ex) {
                AppendTerminal("ERROR", "PC Kapatma başlatılamadı: " + ex.Message, AccentRed);
            }
        }

        void AbortPcShutdown()
        {
            try {
                shutdownTriggered = false;
                var psi = new ProcessStartInfo("shutdown.exe", "/a") {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                AppendTerminal("SHUTDOWN", "✅ Bilgisayar kapatma işlemi iptal edildi (shutdown /a).", AccentEmerald);
                NotifyExternalEvent("SYSTEM", "Kapatma İptal Edildi", 0, null, "Bilgisayar otomatik kapatma işlemi iptal edildi.");
                SendDirectWhatsApp("✅ Umutistan Bot: Bilgisayar kapatma işlemi başarıyla iptal edildi.");
                if (sidebarBrowser != null && sidebarBrowser.CoreWebView2 != null) {
                    sidebarBrowser.CoreWebView2.ExecuteScriptAsync("if(window.onShutdownAborted) window.onShutdownAborted();");
                }
            } catch (Exception ex) {
                AppendTerminal("ERROR", "Kapatma iptal etme hatası: " + ex.Message, AccentRed);
            }
        }

        string GetLocalIpAddress()
        {
            try {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList) {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                        return ip.ToString();
                }
            } catch { }
            return "127.0.0.1";
        }

        void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (!closeConfirmed && e.CloseReason == CloseReason.UserClosing && pages.Any(p => p.Armed)) {
                e.Cancel = true;
                ShowDialog("close-app", null);
                return;
            }
            closing = true;
            pollTimer.Stop();
            try { NotifyExternalEvent("SYSTEM", "Masaüstü Arayüzü Kapatıldı", 0, null, "Umutistan Sniper kullanıcı arayüzü kapatıldı."); } catch { }
            try { if (ipcListener != null) ipcListener.Stop(); } catch { }
            if (topbarBrowser != null) { try { topbarBrowser.Dispose(); } catch { } }
            if (sidebarBrowser != null) { try { sidebarBrowser.Dispose(); } catch { } }
            if (settingsBrowser != null) { try { settingsBrowser.Dispose(); } catch { } }
            if (terminalBrowser != null) terminalBrowser.Dispose();
            if (dialogBrowser != null) dialogBrowser.Dispose();
            foreach (var p in pages) {
                p.Armed = false;
                if (p.Browser != null) p.Browser.Dispose();
            }
        }

        static Button CreateButton(string text, int width, int height, Color bg)
        {
            var b = new Button {
                Text = text, Width = width, Height = height,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White,
                Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        static Button CreateFlatIconBtn(string text, int width, int height, Color bg)
        {
            var b = new Button {
                Text = text, Width = width, Height = height,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White,
                Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        static Button CreateFlatButton(string text, int width, int height, Action click)
        {
            var b = new Button {
                Text = text, Width = width, Height = height,
                FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.Gainsboro,
                Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += delegate { click(); };
            return b;
        }

        static Panel CreateField(string labelText, Control input)
        {
            var p = new Panel { Width = 152, Height = 56, Margin = new Padding(2, 2, 4, 2), BackColor = Color.Transparent };
            var lbl = new Label { Text = labelText, AutoSize = true, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = TextMuted, Location = new Point(2, 2) };
            input.Location = new Point(0, 18);
            p.Controls.Add(lbl);
            p.Controls.Add(input);
            return p;
        }

        static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "—";
            return text.Length > max ? text.Substring(0, max) + "…" : text;
        }

        public sealed class SleekNumericBox : Panel
        {
            private decimal _value;
            private readonly decimal _min, _max, _step;
            private readonly Label _lblVal;
            private readonly Button _btnMinus, _btnPlus;
            private readonly string _suffix;

            public event EventHandler ValueChanged;

            public decimal Value
            {
                get { return _value; }
                set
                {
                    decimal clamped = Math.Max(_min, Math.Min(_max, value));
                    if (_value != clamped)
                    {
                        _value = clamped;
                        UpdateDisplay();
                        if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
                    }
                }
            }

            public decimal Minimum { get { return _min; } }
            public decimal Maximum { get { return _max; } }

            public SleekNumericBox(decimal min, decimal max, decimal initVal, decimal step, string suffix)
            {
                _min = min; _max = max; _step = step; _value = initVal; _suffix = suffix;
                Size = new Size(148, 34);
                BackColor = Color.FromArgb(22, 22, 28);
                Margin = new Padding(0);

                _btnMinus = new Button {
                    Text = "−", Dock = DockStyle.Left, Width = 30, FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(32, 32, 40), ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand
                };
                _btnMinus.FlatAppearance.BorderSize = 0;
                _btnMinus.Click += delegate { Value = Math.Max(_min, _value - _step); };

                _btnPlus = new Button {
                    Text = "+", Dock = DockStyle.Right, Width = 30, FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(32, 32, 40), ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand
                };
                _btnPlus.FlatAppearance.BorderSize = 0;
                _btnPlus.Click += delegate { Value = Math.Min(_max, _value + _step); };

                _lblVal = new Label {
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };

                Controls.Add(_lblVal);
                Controls.Add(_btnMinus);
                Controls.Add(_btnPlus);

                Paint += delegate(object s, PaintEventArgs e) {
                    ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.FromArgb(42, 42, 50), ButtonBorderStyle.Solid);
                };

                UpdateDisplay();
            }

            private void UpdateDisplay()
            {
                _lblVal.Text = _value.ToString(CultureInfo.InvariantCulture) + _suffix;
            }
        }

        sealed class PageItem
        {
            public WebView2 Browser;
            public bool Ready, Armed;
            public decimal Max = 32, Step = 1, Pending;
            public int Seconds = 1, LastSeconds = -1;
            public string Title, Url;
            public string CustomTitle;
            public bool Pinned;
            public Snapshot State;
            public bool LimitWarned;
            public decimal LastWarnedBid;
        }

        public sealed class Snapshot
        {
            public bool IsAuction, IsWinner, IsLost, OwnHighest, LoggedIn, OneClick;
            public decimal CurrentBid, MinimumBid;
            public int Seconds;
            public string TimerText, Title;
        }

        public sealed class SavedState
        {
            public decimal Max = 32, Step = 1;
            public int Seconds = 1;
        }
    }
}
