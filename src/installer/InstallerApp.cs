// ============================================================================
// MARTINISTAN VAVABID ONE-CLICK INSTALLER & UPDATER
// Developed by Martinistan / Martiny (@martinistan)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace UmutistanInstaller
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string installDir = @"C:\VakantieVeilingenBot";
            string mainExe = Path.Combine(installDir, "UmutistanDolandiricilikHizmetleri.exe");
            string settingsFile = Path.Combine(installDir, "settings.json");

            bool forceWizard = false;
            if (args != null) {
                foreach (var a in args) {
                    if (a.Equals("--setup", StringComparison.OrdinalIgnoreCase) || a.Equals("--wizard", StringComparison.OrdinalIgnoreCase))
                        forceWizard = true;
                }
            }

            // EĞER ZATEN KURULU İSE VE WIZARD ZORLANMIYORSA:
            if (!forceWizard && File.Exists(mainExe) && File.Exists(settingsFile))
            {
                try {
                    MartinyWizardForm.QuickUpdatePayload();
                    MartinyWizardForm.EnsureShortcutCreated();
                } catch { }

                try {
                    Process.Start(new ProcessStartInfo(mainExe) {
                        WorkingDirectory = installDir,
                        UseShellExecute = true
                    });
                    return;
                } catch { }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MartinyWizardForm());
        }
    }

    public sealed class MartinyWizardForm : Form
    {
        static readonly Color BgDark = Color.FromArgb(9, 9, 11);
        static readonly Color BgCard = Color.FromArgb(18, 18, 20);
        static readonly Color BgInput = Color.FromArgb(28, 28, 32);
        static readonly Color BorderColor = Color.FromArgb(39, 39, 42);
        static readonly Color AccentEmerald = Color.FromArgb(16, 185, 129);
        static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        static readonly Color TextMuted = Color.FromArgb(161, 161, 170);

        const string InstallDir = @"C:\VakantieVeilingenBot";
        readonly JavaScriptSerializer json = new JavaScriptSerializer();

        int step = 0;
        string localIp = "127.0.0.1";

        // Layout Controls
        readonly Label lblStepIndicator = new Label();
        readonly FlowLayoutPanel flowCard = new FlowLayoutPanel();
        readonly Button btnBack = new Button();
        readonly Button btnQuick = new Button();
        readonly Button btnNext = new Button();

        // Inputs
        readonly TextBox txtDiscordToken = new TextBox();
        readonly TextBox txtGuildId = new TextBox();
        readonly CheckBox chkWhatsApp = new CheckBox();
        readonly TextBox txtWhatsAppPhone = new TextBox();
        readonly TextBox txtWhatsAppApiKey = new TextBox();
        readonly TextBox txtWebPin = new TextBox();
        readonly TextBox txtWebPort = new TextBox();
        readonly TextBox txtDefaultMax = new TextBox();
        readonly TextBox txtDefaultStep = new TextBox();
        readonly TextBox txtDefaultSec = new TextBox();

        public MartinyWizardForm()
        {
            Text = "Martinistan Vavabid Sniper Suite - Setup Assistant";
            Size = new Size(740, 640);
            MinimumSize = new Size(740, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgDark;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            try {
                if (File.Exists("app-logo.ico")) Icon = new Icon("app-logo.ico");
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            } catch { }

            DetectLocalIp();
            InitializeInputs();
            BuildUI();
            ShowStep(0);
        }

        void DetectLocalIp()
        {
            try {
                using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                    s.Connect("8.8.8.8", 80);
                    var ep = s.LocalEndPoint as IPEndPoint;
                    if (ep != null) localIp = ep.Address.ToString();
                }
            } catch {
                try {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList) {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127.")) {
                            localIp = ip.ToString();
                            break;
                        }
                    }
                } catch { localIp = "127.0.0.1"; }
            }
        }

        void InitializeInputs()
        {
            ConfigureTextBox(txtDiscordToken, true);
            ConfigureTextBox(txtGuildId, false);
            ConfigureTextBox(txtWhatsAppPhone, false);
            ConfigureTextBox(txtWhatsAppApiKey, false);

            chkWhatsApp.Text = "WhatsApp bildirimlerini aktif tut (İhale kazanıldığında anında mesaj ilet)";
            chkWhatsApp.ForeColor = AccentEmerald;
            chkWhatsApp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkWhatsApp.Checked = true;
            chkWhatsApp.AutoSize = true;
            chkWhatsApp.Margin = new Padding(0, 0, 0, 10);

            ConfigureTextBox(txtWebPin, false);
            txtWebPin.Width = 140;
            txtWebPin.MaxLength = 4;
            txtWebPin.Text = "1234";
            txtWebPin.TextAlign = HorizontalAlignment.Center;
            txtWebPin.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            txtWebPin.ForeColor = AccentEmerald;

            ConfigureTextBox(txtWebPort, false);
            txtWebPort.Width = 140;
            txtWebPort.Text = "5000";

            ConfigureTextBox(txtDefaultMax, false);
            txtDefaultMax.Text = "32";

            ConfigureTextBox(txtDefaultStep, false);
            txtDefaultStep.Text = "1";

            ConfigureTextBox(txtDefaultSec, false);
            txtDefaultSec.Text = "1";

            LoadExistingConfigIfAny();
        }

        void LoadExistingConfigIfAny()
        {
            try {
                string cfgFile = Path.Combine(InstallDir, "settings.json");
                if (File.Exists(cfgFile)) {
                    var dict = json.Deserialize<Dictionary<string, string>>(File.ReadAllText(cfgFile));
                    if (dict != null) {
                        if (dict.ContainsKey("discord_token") && !string.IsNullOrWhiteSpace(dict["discord_token"])) txtDiscordToken.Text = dict["discord_token"];
                        if (dict.ContainsKey("guild_id") && !string.IsNullOrWhiteSpace(dict["guild_id"])) txtGuildId.Text = dict["guild_id"];
                        if (dict.ContainsKey("whatsapp_enabled")) chkWhatsApp.Checked = (dict["whatsapp_enabled"] == "1");
                        if (dict.ContainsKey("whatsapp_target_phone") && !string.IsNullOrWhiteSpace(dict["whatsapp_target_phone"])) txtWhatsAppPhone.Text = dict["whatsapp_target_phone"];
                        if (dict.ContainsKey("whatsapp_apikey") && !string.IsNullOrWhiteSpace(dict["whatsapp_apikey"])) txtWhatsAppApiKey.Text = dict["whatsapp_apikey"];
                        if (dict.ContainsKey("web_pin") && !string.IsNullOrWhiteSpace(dict["web_pin"])) txtWebPin.Text = dict["web_pin"];
                        if (dict.ContainsKey("web_port") && !string.IsNullOrWhiteSpace(dict["web_port"])) txtWebPort.Text = dict["web_port"];
                        if (dict.ContainsKey("default_max_bid") && !string.IsNullOrWhiteSpace(dict["default_max_bid"])) txtDefaultMax.Text = dict["default_max_bid"];
                        if (dict.ContainsKey("default_increment") && !string.IsNullOrWhiteSpace(dict["default_increment"])) txtDefaultStep.Text = dict["default_increment"];
                        if (dict.ContainsKey("default_seconds") && !string.IsNullOrWhiteSpace(dict["default_seconds"])) txtDefaultSec.Text = dict["default_seconds"];
                    }
                }
            } catch { }
        }

        void ConfigureTextBox(TextBox tb, bool isPassword)
        {
            tb.BackColor = BgInput;
            tb.ForeColor = Color.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Segoe UI", 10.5f);
            tb.Height = 28;
            if (isPassword) tb.UseSystemPasswordChar = true;
        }

        void BuildUI()
        {
            // 1. Header Banner
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = BgCard, Padding = new Padding(24, 14, 24, 10) };
            var lblTitle = new Label {
                Text = "◈  UMUTİSTAN OTONOM KURULUM ASİSTANI",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = AccentEmerald,
                AutoSize = true,
                Location = new Point(24, 12)
            };
            var lblSub = new Label {
                Text = "VakantieVeilingen Sniper & Otonom Gateway Akıllı Kurulum Sistemi",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(26, 40)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // 2. Step Indicator Bar
            lblStepIndicator.Dock = DockStyle.Top;
            lblStepIndicator.Height = 36;
            lblStepIndicator.BackColor = BgDark;
            lblStepIndicator.ForeColor = AccentBlue;
            lblStepIndicator.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblStepIndicator.Padding = new Padding(24, 8, 24, 0);

            // 3. Central Card Area (FlowLayoutPanel)
            flowCard.Dock = DockStyle.Fill;
            flowCard.BackColor = BgCard;
            flowCard.FlowDirection = FlowDirection.TopDown;
            flowCard.WrapContents = false;
            flowCard.AutoScroll = true;
            flowCard.Padding = new Padding(24, 20, 24, 20);
            flowCard.Paint += delegate(object s, PaintEventArgs e) {
                ControlPaint.DrawBorder(e.Graphics, flowCard.ClientRectangle, BorderColor, ButtonBorderStyle.Solid);
            };

            var pnlCenter = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(24, 0, 24, 12) };
            pnlCenter.Controls.Add(flowCard);

            // 4. Bottom Navigation
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = BgDark, Padding = new Padding(24, 10, 24, 14) };

            btnBack.Text = "◀ Geri";
            btnBack.Size = new Size(110, 40);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.BackColor = BgInput;
            btnBack.ForeColor = Color.Gainsboro;
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Cursor = Cursors.Hand;
            btnBack.Location = new Point(24, 10);
            btnBack.Click += delegate { if (step > 0) ShowStep(step - 1); };

            btnQuick.Text = "⚡ Hızlı Kurulum (Varsayılanlar)";
            btnQuick.Size = new Size(230, 40);
            btnQuick.FlatStyle = FlatStyle.Flat;
            btnQuick.BackColor = Color.FromArgb(24, 24, 27);
            btnQuick.ForeColor = Color.DarkGray;
            btnQuick.FlatAppearance.BorderSize = 0;
            btnQuick.Cursor = Cursors.Hand;
            btnQuick.Location = new Point(148, 10);
            btnQuick.Click += delegate { ExecuteInstallation(); };

            btnNext.Text = "İleri ▶";
            btnNext.Size = new Size(210, 40);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.BackColor = AccentEmerald;
            btnNext.ForeColor = Color.Black;
            btnNext.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Cursor = Cursors.Hand;
            btnNext.Location = new Point(486, 10);
            btnNext.Click += delegate {
                if (step < 3) ShowStep(step + 1);
                else ExecuteInstallation();
            };

            pnlBottom.Controls.Add(btnBack);
            pnlBottom.Controls.Add(btnQuick);
            pnlBottom.Controls.Add(btnNext);

            Controls.Add(pnlCenter);
            Controls.Add(lblStepIndicator);
            Controls.Add(pnlHeader);
            Controls.Add(pnlBottom);
        }

        void ShowStep(int newStep)
        {
            step = newStep;
            flowCard.Controls.Clear();

            btnBack.Enabled = (step > 0);
            if (step == 3) {
                btnNext.Text = "⚡ SİSTEMİ KUR & BAŞLAT";
                btnNext.BackColor = AccentEmerald;
                btnNext.ForeColor = Color.Black;
            } else {
                btnNext.Text = "İleri ▶";
                btnNext.BackColor = AccentEmerald;
                btnNext.ForeColor = Color.Black;
            }

            if (step == 0) {
                lblStepIndicator.Text = "ADIM 1 / 4  ◈  DISCORD BOT VE BİLDİRİM YAPILANDIRMASI";

                var bubble = CreateAiBubble(
                    "Merhaba! VakantieVeilingen açık artırmalarını saniyenin onda biri hassasiyetle\n" +
                    "kazanmak için sistem ortamınızı birlikte yapılandıralım.\n\n" +
                    "Discord botunuz üzerinden anlık 4K ekran görüntüleri ve teklif bildirimleri almak\n" +
                    "istiyorsanız bilgilerinizi girin (İsteğe bağlıdır, boş bırakıp doğrudan geçebilirsiniz):"
                );
                flowCard.Controls.Add(bubble);

                flowCard.Controls.Add(CreateInputField("DISCORD BOT TOKEN:", txtDiscordToken, "Botunuzun Developer Portal'dan aldığınız özel tokenı (Şifrelenmiş saklanır)"));
                flowCard.Controls.Add(CreateInputField("DISCORD SUNUCU (GUILD) ID:", txtGuildId, "Botun özel kanalları açacağı Discord sunucusunun sayısal ID'si"));
            }
            else if (step == 1) {
                lblStepIndicator.Text = "ADIM 2 / 4  ◈  WHATSAPP ANLIK BİLDİRİM ENTEGRASYONU";

                var bubble = CreateAiBubble(
                    "Bir açık artırmayı kazandığınızda veya teklif verildiğinde doğrudan cep telefonunuza\n" +
                    "ücretsiz WhatsApp mesajı iletebilirim.\n\n" +
                    "CallMeBot (ücretsiz kişisel WhatsApp API) veya telefon numaranızı belirtebilirsiniz:"
                );
                flowCard.Controls.Add(bubble);

                flowCard.Controls.Add(chkWhatsApp);
                flowCard.Controls.Add(CreateInputField("HEDEF TELEFON NUMARASI (+90... VEYA +31...):", txtWhatsAppPhone, "Ülke koduyla birlikte giriniz (Örn: +905321234567 veya +31612345678)"));
                flowCard.Controls.Add(CreateInputField("CALLMEBOT API KEY (OPSİYONEL / ÜCRETSİZ):", txtWhatsAppApiKey, "CallMeBot ücretsiz WhatsApp bildirim anahtarı (Varsa girin, yoksa boş bırakabilirsiniz)"));
            }
            else if (step == 2) {
                lblStepIndicator.Text = "ADIM 3 / 4  ◈  MOBİL WEB DASHBOARD & GÜVENLİK";

                var bubble = CreateAiBubble(
                    "Aynı ev/ofis WiFi ağına bağlı cep telefonunuzdan veya tabletinizden bilgisayar başında\n" +
                    "olmadan ihaleleri izleyebilir ve anında yeni açık artırma ekleyebilirsiniz.\n\n" +
                    "Güvenliğiniz için 4 haneli bir mobil PIN kodu belirleyin:"
                );
                flowCard.Controls.Add(bubble);

                flowCard.Controls.Add(CreateInputField("4 HANELİ MOBİL GİRİŞ PIN KODU:", txtWebPin, "Telefondan bağlandığınızda sorulacak şifre (Varsayılan: 1234)"));
                flowCard.Controls.Add(CreateIpBox(localIp, "5000"));
            }
            else if (step == 3) {
                lblStepIndicator.Text = "ADIM 4 / 4  ◈  İHALE STRATEJİSİ VE HESAP TAVSİYELERİ";

                var bubble = CreateAiBubble(
                    "Kurulum tamamlanmak üzere! Son olarak önemli ihale ve oturum tavsiyeleri:\n\n" +
                    "1. Uygulama açıldığında ilk sekmede VakantieVeilingen hesabınıza bir defaya mahsus giriş yapın.\n" +
                    "   Oturum çerezleriniz C:\\VakantieVeilingenBot\\Profile altında kalıcı ve şifreli saklanır.\n" +
                    "2. İhale varsayılan üst limit ve sniper tetikleme süreleri aşağıda ayarlanmıştır:"
                );
                flowCard.Controls.Add(bubble);

                flowCard.Controls.Add(CreateAuctionRulesPanel());

                var pnlSuccessNotice = new Panel {
                    Width = 640,
                    AutoSize = true,
                    BackColor = Color.FromArgb(16, 40, 28),
                    Padding = new Padding(14, 12, 14, 12),
                    Margin = new Padding(0, 8, 0, 10)
                };
                pnlSuccessNotice.Paint += delegate(object s, PaintEventArgs e) {
                    ControlPaint.DrawBorder(e.Graphics, pnlSuccessNotice.ClientRectangle, AccentEmerald, ButtonBorderStyle.Solid);
                };
                var lblNote = new Label {
                    Text = "✓ 'SİSTEMİ KUR & BAŞLAT' butonuna bastığınızda tüm dosyalar çıkarılacak,\n" +
                           "  masaüstü kısayolu oluşturulacak ve otonom sistem anında çalıştırılacaktır.",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 222, 128),
                    AutoSize = true
                };
                pnlSuccessNotice.Controls.Add(lblNote);
                flowCard.Controls.Add(pnlSuccessNotice);
            }
        }

        Panel CreateAiBubble(string text)
        {
            var pnl = new Panel {
                Width = 640,
                AutoSize = true,
                BackColor = Color.FromArgb(24, 24, 28),
                Padding = new Padding(16, 14, 16, 14),
                Margin = new Padding(0, 0, 0, 16)
            };
            pnl.Paint += delegate(object s, PaintEventArgs e) {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(45, 45, 52), ButtonBorderStyle.Solid);
            };

            var lbl = new Label {
                Text = text,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(244, 244, 245),
                AutoSize = true,
                MaximumSize = new Size(608, 0),
                Location = new Point(16, 14)
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        Panel CreateInputField(string labelText, Control inputControl, string helpText)
        {
            var pnl = new Panel {
                Width = 640,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };

            var lbl = new Label {
                Text = labelText,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            pnl.Controls.Add(lbl);

            inputControl.Location = new Point(0, 22);
            if (inputControl.Width == 0 || inputControl.Width > 500) {
                inputControl.Width = 636;
            }
            pnl.Controls.Add(inputControl);

            int nextTop = 22 + inputControl.Height + 4;
            if (!string.IsNullOrEmpty(helpText)) {
                var lblHelp = new Label {
                    Text = helpText,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(113, 113, 122),
                    AutoSize = true,
                    Location = new Point(0, nextTop)
                };
                pnl.Controls.Add(lblHelp);
            }
            return pnl;
        }

        Panel CreateIpBox(string ip, string port)
        {
            var pnl = new Panel {
                Width = 640,
                Height = 72,
                BackColor = Color.FromArgb(30, 27, 75),
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 4, 0, 14)
            };
            pnl.Paint += delegate(object s, PaintEventArgs e) {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Color.FromArgb(67, 56, 202), ButtonBorderStyle.Solid);
            };

            var l1 = new Label {
                Text = "TELEFONUNUZDAN VEYA TABLETİNİZDEN BAĞLANACAĞINIZ CANLI WIFI ADRESİ:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(165, 180, 252),
                Location = new Point(14, 10),
                AutoSize = true
            };
            var l2 = new Label {
                Text = "http://" + ip + ":" + port,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(14, 34),
                AutoSize = true
            };
            pnl.Controls.Add(l1);
            pnl.Controls.Add(l2);
            return pnl;
        }

        Panel CreateAuctionRulesPanel()
        {
            var pnl = new Panel {
                Width = 640,
                Height = 62,
                Margin = new Padding(0, 0, 0, 14)
            };

            var l1 = new Label { Text = "ÜST LİMİT (€):", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = TextMuted, Location = new Point(0, 0), AutoSize = true };
            txtDefaultMax.Location = new Point(0, 22);
            txtDefaultMax.Width = 190;

            var l2 = new Label { Text = "ARTIŞ (+€):", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = TextMuted, Location = new Point(220, 0), AutoSize = true };
            txtDefaultStep.Location = new Point(220, 22);
            txtDefaultStep.Width = 190;

            var l3 = new Label { Text = "SNIPER TETİKLEME (SN):", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = TextMuted, Location = new Point(440, 0), AutoSize = true };
            txtDefaultSec.Location = new Point(440, 22);
            txtDefaultSec.Width = 190;

            pnl.Controls.Add(l1); pnl.Controls.Add(txtDefaultMax);
            pnl.Controls.Add(l2); pnl.Controls.Add(txtDefaultStep);
            pnl.Controls.Add(l3); pnl.Controls.Add(txtDefaultSec);
            return pnl;
        }

        void ExecuteInstallation()
        {
            flowCard.Controls.Clear();
            btnBack.Enabled = false;
            btnNext.Enabled = false;
            btnQuick.Enabled = false;

            lblStepIndicator.Text = "SİSTEM KURULUMU GERÇEKLEŞTİRİLİYOR...";

            var lblStatus = new Label {
                Text = "◈  Umutistan AI Sistemi Yapılandırıyor...",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = AccentEmerald,
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 14)
            };
            flowCard.Controls.Add(lblStatus);

            var prog = new ProgressBar {
                Size = new Size(636, 26),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                Margin = new Padding(0, 0, 0, 12)
            };
            flowCard.Controls.Add(prog);

            var lblDetail = new Label {
                Text = "Sistem dizinleri C:\\VakantieVeilingenBot altına kuruluyor...",
                ForeColor = TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            flowCard.Controls.Add(lblDetail);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    UpdateProgress(prog, lblDetail, 20, "Dizinler açılıyor...");
                    Directory.CreateDirectory(InstallDir);
                    Directory.CreateDirectory(Path.Combine(InstallDir, "Profile"));
                    Directory.CreateDirectory(Path.Combine(InstallDir, "Screenshots"));
                    Directory.CreateDirectory(Path.Combine(InstallDir, "logs"));

                    UpdateProgress(prog, lblDetail, 40, "Bileşenler ve WebView2 çıkarılıyor...");
                    ExtractEmbeddedPayload();

                    UpdateProgress(prog, lblDetail, 65, "Yapılandırma dosyaları ve ayarlar kaydediliyor...");
                    SaveConfigToFileAndDb();

                    UpdateProgress(prog, lblDetail, 80, "Masaüstü kısayolu oluşturuluyor...");
                    CreateDesktopShortcut();

                    UpdateProgress(prog, lblDetail, 90, "Arka plan servisleri başlatılıyor...");
                    StartServices();

                    UpdateProgress(prog, lblDetail, 100, "Tamamlandı! Sistem açılıyor...");
                    Thread.Sleep(1000);

                    BeginInvoke(new Action(delegate {
                        Close();
                    }));
                }
                catch (Exception ex) {
                    BeginInvoke(new Action(delegate {
                        MessageBox.Show(this, "Kurulum sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    }));
                }
            });
        }

        void UpdateProgress(ProgressBar prog, Label lbl, int val, string text)
        {
            BeginInvoke(new Action(delegate {
                prog.Value = val;
                lbl.Text = text;
            }));
            Thread.Sleep(300);
        }

        public static void QuickUpdatePayload()
        {
            ExtractEmbeddedPayload();
        }

        public static void EnsureShortcutCreated()
        {
            CreateDesktopShortcut();
        }

        public static void ExtractEmbeddedPayload()
        {
            var asm = Assembly.GetExecutingAssembly();
            Stream zipStream = null;

            string[] names = asm.GetManifestResourceNames();
            foreach (var n in names) {
                if (n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase)) {
                    zipStream = asm.GetManifestResourceStream(n);
                    break;
                }
            }

            string tempZip = Path.Combine(InstallDir, "temp_payload.zip");
            if (zipStream != null) {
                using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write)) {
                    zipStream.CopyTo(fs);
                }
            } else if (File.Exists("payload.zip")) {
                File.Copy("payload.zip", tempZip, true);
            }

            if (File.Exists(tempZip)) {
                using (var archive = ZipFile.OpenRead(tempZip)) {
                    foreach (var entry in archive.Entries) {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string dest = Path.Combine(InstallDir, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        if (entry.Name.Equals("database.db", StringComparison.OrdinalIgnoreCase) && File.Exists(dest)) {
                            continue;
                        }
                        if (entry.Name.Equals("settings.json", StringComparison.OrdinalIgnoreCase) && File.Exists(dest)) {
                            continue;
                        }
                        entry.ExtractToFile(dest, true);
                    }
                }
                try { File.Delete(tempZip); } catch { }
            }
        }

        void SaveConfigToFileAndDb()
        {
            string cfgFile = Path.Combine(InstallDir, "settings.json");
            var dict = new Dictionary<string, string>();
            if (File.Exists(cfgFile)) {
                try {
                    dict = json.Deserialize<Dictionary<string, string>>(File.ReadAllText(cfgFile)) ?? new Dictionary<string, string>();
                } catch { }
            }

            if (!string.IsNullOrWhiteSpace(txtDiscordToken.Text)) dict["discord_token"] = txtDiscordToken.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtGuildId.Text)) dict["guild_id"] = txtGuildId.Text.Trim();
            dict["whatsapp_enabled"] = chkWhatsApp.Checked ? "1" : "0";
            if (!string.IsNullOrWhiteSpace(txtWhatsAppPhone.Text)) dict["whatsapp_target_phone"] = txtWhatsAppPhone.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtWhatsAppApiKey.Text)) dict["whatsapp_apikey"] = txtWhatsAppApiKey.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtWebPin.Text)) dict["web_pin"] = txtWebPin.Text.Trim();
            else if (!dict.ContainsKey("web_pin")) dict["web_pin"] = "1234";

            if (!string.IsNullOrWhiteSpace(txtWebPort.Text)) dict["web_port"] = txtWebPort.Text.Trim();
            else if (!dict.ContainsKey("web_port")) dict["web_port"] = "5000";

            if (!string.IsNullOrWhiteSpace(txtDefaultMax.Text)) dict["default_max_bid"] = txtDefaultMax.Text.Trim();
            else if (!dict.ContainsKey("default_max_bid")) dict["default_max_bid"] = "40";

            if (!string.IsNullOrWhiteSpace(txtDefaultStep.Text)) dict["default_increment"] = txtDefaultStep.Text.Trim();
            else if (!dict.ContainsKey("default_increment")) dict["default_increment"] = "1";

            if (!string.IsNullOrWhiteSpace(txtDefaultSec.Text)) dict["default_seconds"] = txtDefaultSec.Text.Trim();
            else if (!dict.ContainsKey("default_seconds")) dict["default_seconds"] = "2";

            dict["stop_group_on_first_win"] = "1";
            File.WriteAllText(cfgFile, json.Serialize(dict));
        }

        public static void CreateDesktopShortcut()
        {
            try {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string onedrive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Desktop");
                string targetDir = Directory.Exists(onedrive) ? onedrive : desktop;

                string linkPath = Path.Combine(targetDir, "Umutistan Dolandiricilik Hizmetleri.lnk");
                string exePath = Path.Combine(InstallDir, "UmutistanDolandiricilikHizmetleri.exe");
                string iconPath = Path.Combine(InstallDir, "app-logo.ico");

                string vbs = "Set oWS = WScript.CreateObject(\"WScript.Shell\")\n" +
                             "sLinkFile = \"" + linkPath + "\"\n" +
                             "Set oLink = oWS.CreateShortcut(sLinkFile)\n" +
                             "oLink.TargetPath = \"" + exePath + "\"\n" +
                             "oLink.WorkingDirectory = \"" + InstallDir + "\"\n" +
                             "oLink.IconLocation = \"" + iconPath + "\"\n" +
                             "oLink.Save";

                string vbsFile = Path.Combine(InstallDir, "shortcut.vbs");
                File.WriteAllText(vbsFile, vbs);
                var p = Process.Start(new ProcessStartInfo("cscript", "//nologo \"" + vbsFile + "\"") {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (p != null) p.WaitForExit();
                try { File.Delete(vbsFile); } catch { }
            } catch { }
        }

        void StartServices()
        {
            try {
                string botExe = Path.Combine(InstallDir, "BotService.exe");
                if (File.Exists(botExe)) {
                    Process.Start(new ProcessStartInfo(botExe) {
                        WorkingDirectory = InstallDir,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            } catch { }

            Thread.Sleep(1000);

            try {
                string mainExe = Path.Combine(InstallDir, "UmutistanDolandiricilikHizmetleri.exe");
                if (File.Exists(mainExe)) {
                    Process.Start(new ProcessStartInfo(mainExe) {
                        WorkingDirectory = InstallDir
                    });
                }
            } catch { }
        }
    }
}
