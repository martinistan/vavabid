"""
Umutistan Dolandiricilik Hizmetleri - AI Onboarding & Monolithic Installer
Tek bir bağımsız .exe içinde AI tarzı etkileşimli kurulum sihirbazı ve sistem başlatıcı.
"""

import os
import sys
import zipfile
import subprocess
import sqlite3
import json
import time
import socket
import urllib.parse
import urllib.request
import tkinter as tk
from tkinter import ttk, messagebox

INSTALL_DIR = r"C:\VakantieVeilingenBot"

def get_bundle_dir():
    if getattr(sys, 'frozen', False):
        return sys._MEIPASS
    return os.path.dirname(os.path.abspath(__file__))

def get_local_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except:
        return "127.0.0.1"

def perform_installation(config_data, progress_callback=None):
    if progress_callback: progress_callback(10, "Sistem dizinleri hazirlaniyor...")
    os.makedirs(INSTALL_DIR, exist_ok=True)
    os.makedirs(os.path.join(INSTALL_DIR, "Profile"), exist_ok=True)
    os.makedirs(os.path.join(INSTALL_DIR, "Screenshots"), exist_ok=True)
    os.makedirs(os.path.join(INSTALL_DIR, "logs"), exist_ok=True)

    # 1. Payload Zip'i cikar
    if progress_callback: progress_callback(30, "Uygulama bilesenleri ve DLL'ler cikariliyor...")
    bundle_dir = get_bundle_dir()
    zip_path = os.path.join(bundle_dir, "payload.zip")
    if not os.path.exists(zip_path):
        zip_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "payload.zip")

    if os.path.exists(zip_path):
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            zip_ref.extractall(INSTALL_DIR)

    # 2. Veritabani olustur ve yapilandir
    if progress_callback: progress_callback(55, "SQLite WAL veritabani hazirlaniyor...")
    db_path = os.path.join(INSTALL_DIR, "database.db")
    conn = sqlite3.connect(db_path)
    cur = conn.cursor()
    cur.execute("PRAGMA journal_mode=WAL;")
    cur.execute("""
    CREATE TABLE IF NOT EXISTS config (
        key TEXT PRIMARY KEY,
        value TEXT,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    cur.execute("""
    CREATE TABLE IF NOT EXISTS auctions (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        url TEXT UNIQUE NOT NULL,
        title TEXT DEFAULT 'Yeni Ihale',
        max_bid REAL DEFAULT 40.0,
        increment REAL DEFAULT 1.0,
        trigger_seconds INTEGER DEFAULT 2,
        current_bid REAL DEFAULT 0.0,
        next_bid REAL DEFAULT 0.0,
        seconds_left INTEGER DEFAULT -1,
        status TEXT DEFAULT 'BEKLIYOR',
        is_active INTEGER DEFAULT 0,
        is_won INTEGER DEFAULT 0,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    cur.execute("""
    CREATE TABLE IF NOT EXISTS bids (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        auction_id INTEGER,
        bid_amount REAL,
        previous_amount REAL,
        status TEXT,
        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    cur.execute("""
    CREATE TABLE IF NOT EXISTS wins (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        auction_id INTEGER,
        title TEXT,
        final_price REAL,
        screenshot_path TEXT,
        discord_sent INTEGER DEFAULT 0,
        whatsapp_sent INTEGER DEFAULT 0,
        won_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    cur.execute("""
    CREATE TABLE IF NOT EXISTS logs (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        category TEXT DEFAULT 'SISTEM',
        message TEXT,
        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)

    # Ayarlari veritabanina yaz
    for k, v in config_data.items():
        cur.execute("INSERT OR REPLACE INTO config (key, value) VALUES (?, ?)", (k, str(v)))
    conn.commit()
    conn.close()

    # 3. settings.json olustur
    cfg_file = os.path.join(INSTALL_DIR, "settings.json")
    with open(cfg_file, "w", encoding="utf-8") as f:
        json.dump(config_data, f, indent=4)

    # 4. Masaustu Kisayolu Olustur
    if progress_callback: progress_callback(75, "Masaustu kisayolu olusturuluyor...")
    try:
        desktop = os.path.join(os.path.expanduser("~"), "Desktop")
        onedrive_desktop = os.path.join(os.path.expanduser("~"), "OneDrive", "Desktop")
        target_desktop = onedrive_desktop if os.path.exists(onedrive_desktop) else desktop

        shortcut_path = os.path.join(target_desktop, "Umutistan Dolandiricilik Hizmetleri.lnk")
        exe_path = os.path.join(INSTALL_DIR, "UmutistanDolandiricilikHizmetleri.exe")
        icon_path = os.path.join(INSTALL_DIR, "app-logo.ico")

        vbs = f"""
        Set oWS = WScript.CreateObject("WScript.Shell")
        sLinkFile = "{shortcut_path}"
        Set oLink = oWS.CreateShortcut(sLinkFile)
        oLink.TargetPath = "{exe_path}"
        oLink.WorkingDirectory = "{INSTALL_DIR}"
        oLink.IconLocation = "{icon_path}"
        oLink.Save
        """
        vbs_file = os.path.join(INSTALL_DIR, "create_shortcut.vbs")
        with open(vbs_file, "w", encoding="utf-8") as f:
            f.write(vbs)
        subprocess.run(["cscript", "//nologo", vbs_file], shell=True)
        try: os.remove(vbs_file)
        except: pass
    except Exception as e:
        print(f"Kisayol hatasi: {e}")

    # 5. Arka plan servisini (Discord + Web + WA) baslat
    if progress_callback: progress_callback(90, "Arka plan servisleri baslatiliyor...")
    bot_service_exe = os.path.join(INSTALL_DIR, "BotService.exe")
    if os.path.exists(bot_service_exe):
        subprocess.Popen([bot_service_exe], cwd=INSTALL_DIR, creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0)

    time.sleep(1.0)

    # 6. Ana Masaustu Uygulamasini baslat
    if progress_callback: progress_callback(100, "Tamamlandi! Sistem baslatiliyor...")
    main_exe = os.path.join(INSTALL_DIR, "UmutistanDolandiricilikHizmetleri.exe")
    if os.path.exists(main_exe):
        subprocess.Popen([main_exe], cwd=INSTALL_DIR)


class AIOnboardingApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Umutistan Dolandırılıcılık Hizmetleri - Otonom Kurulum Asistanı")
        self.root.geometry("620x540")
        self.root.configure(bg="#09090b")
        self.root.resizable(False, False)

        try:
            bundle = get_bundle_dir()
            icon_p = os.path.join(bundle, "app-logo.ico")
            if os.path.exists(icon_p):
                self.root.iconbitmap(icon_p)
        except:
            pass

        self.step = 0
        self.local_ip = get_local_ip()

        # Config state
        self.cfg_discord_token = tk.StringVar(value="")
        self.cfg_guild_id = tk.StringVar(value="")
        self.cfg_wa_enabled = tk.StringVar(value="1")
        self.cfg_wa_phone = tk.StringVar(value="")
        self.cfg_wa_apikey = tk.StringVar(value="")
        self.cfg_web_pin = tk.StringVar(value="1234")
        self.cfg_web_port = tk.StringVar(value="5000")
        self.cfg_default_max = tk.StringVar(value="40.0")
        self.cfg_default_step = tk.StringVar(value="1.0")
        self.cfg_default_sec = tk.StringVar(value="2")

        self.setup_ui()
        self.show_step(0)

    def setup_ui(self):
        # Header banner
        header = tk.Frame(self.root, bg="#121214", height=70, padx=20, pady=12)
        header.pack(fill="x")

        title_lbl = tk.Label(header, text="👑 UMUTİSTAN OTONOM ASİSTAN", font=("Segoe UI", 13, "bold"), fg="#10b981", bg="#121214")
        title_lbl.pack(anchor="w")

        sub_lbl = tk.Label(header, text="🤖 VakantieVeilingen Sniper & Gateway Akıllı Kurulum Sihirbazı", font=("Segoe UI", 9), fg="#a1a1aa", bg="#121214")
        sub_lbl.pack(anchor="w")

        # Step indicator
        self.step_indicator = tk.Label(self.root, text="", font=("Segoe UI", 9, "bold"), fg="#3b82f6", bg="#09090b", pady=8)
        self.step_indicator.pack(fill="x")

        # Content card area
        self.card_container = tk.Frame(self.root, bg="#121214", padx=24, pady=20, highlightbackground="#27272a", highlightthickness=1)
        self.card_container.pack(fill="both", expand=True, padx=20, pady=(0, 10))

        # Bottom navigation
        nav_frame = tk.Frame(self.root, bg="#09090b", padx=20, pady=10)
        nav_frame.pack(fill="x", side="bottom")

        self.btn_back = tk.Button(nav_frame, text="◀ Geri", font=("Segoe UI", 9, "bold"), bg="#1c1c20", fg="#d4d4d8", relief="flat", width=12, command=self.prev_step)
        self.btn_back.pack(side="left")

        self.btn_quick = tk.Button(nav_frame, text="⚡ Hızlı Kurulum (Varsayılanlar)", font=("Segoe UI", 9), bg="#18181b", fg="#71717a", relief="flat", command=self.quick_install)
        self.btn_quick.pack(side="left", padx=10)

        self.btn_next = tk.Button(nav_frame, text="İleri ▶", font=("Segoe UI", 9, "bold"), bg="#10b981", fg="#000000", relief="flat", width=16, command=self.next_step)
        self.btn_next.pack(side="right")

    def clear_card(self):
        for widget in self.card_container.winfo_children():
            widget.destroy()

    def show_step(self, step):
        self.step = step
        self.clear_card()

        if step == 0:
            self.step_indicator.config(text="Adım 1 / 4 • 🤖 Hoş Geldiniz & Discord Yapılandırması")
            self.btn_back.config(state="disabled")
            self.btn_next.config(text="İleri ▶", bg="#10b981", fg="#000000")

            ai_bubble = tk.Label(self.card_container, 
                text="🤖 Umutistan AI: Merhaba! VakantieVeilingen açık artırmalarını saniyenin onda biri\n"
                     "hassasiyetle kazanmak için sisteminizi birlikte ayarlayalım.\n\n"
                     "Discord botunuz üzerinden anlık 4K ekran görüntüleri ve teklif bildirimleri\n"
                     "almak istiyorsanız token bilgilerinizi girin (İsteğe bağlıdır, sonradan da eklenebilir):",
                font=("Segoe UI", 9), fg="#e4e4e7", bg="#121214", justify="left")
            ai_bubble.pack(anchor="w", pady=(0, 15))

            tk.Label(self.card_container, text="Discord Bot Token:", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            ent_token = tk.Entry(self.card_container, textvariable=self.cfg_discord_token, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", insertbackground="#ffffff", relief="flat", show="•")
            ent_token.pack(fill="x", pady=(4, 12), ipady=4)

            tk.Label(self.card_container, text="Discord Sunucu (Guild) ID:", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            ent_guild = tk.Entry(self.card_container, textvariable=self.cfg_guild_id, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", insertbackground="#ffffff", relief="flat")
            ent_guild.pack(fill="x", pady=(4, 8), ipady=4)

            tip = tk.Label(self.card_container, text="💡 İpucu: Discord botunuz henüz hazır değilse boş bırakıp doğrudan ileri diyebilirsiniz.", font=("Segoe UI", 8), fg="#71717a", bg="#121214")
            tip.pack(anchor="w")

        elif step == 1:
            self.step_indicator.config(text="Adım 2 / 4 • 📱 WhatsApp Anlık Bildirimleri")
            self.btn_back.config(state="normal")
            self.btn_next.config(text="İleri ▶", bg="#10b981", fg="#000000")

            ai_bubble = tk.Label(self.card_container, 
                text="🤖 Umutistan AI: Bir ihaleyi kazandığınızda veya teklif verildiğinde doğrudan\n"
                     "cep telefonunuza ücretsiz WhatsApp mesajı iletebilirim.\n\n"
                     "CallMeBot (ücretsiz WhatsApp API) veya telefon numaranızı belirtebilirsiniz:",
                font=("Segoe UI", 9), fg="#e4e4e7", bg="#121214", justify="left")
            ai_bubble.pack(anchor="w", pady=(0, 14))

            chk_wa = tk.Checkbutton(self.card_container, text="WhatsApp bildirimlerini aktif tut", variable=self.cfg_wa_enabled, onvalue="1", offvalue="0",
                                    font=("Segoe UI", 9, "bold"), fg="#10b981", bg="#121214", selectcolor="#1c1c20", activebackground="#121214", activeforeground="#10b981")
            chk_wa.pack(anchor="w", pady=(0, 10))

            tk.Label(self.card_container, text="Hedef Telefon Numarası (+90... veya +31...):", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            ent_phone = tk.Entry(self.card_container, textvariable=self.cfg_wa_phone, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", insertbackground="#ffffff", relief="flat")
            ent_phone.pack(fill="x", pady=(4, 12), ipady=4)

            tk.Label(self.card_container, text="CallMeBot API Key (Opsiyonel / Ücretsiz):", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            ent_key = tk.Entry(self.card_container, textvariable=self.cfg_wa_apikey, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", insertbackground="#ffffff", relief="flat")
            ent_key.pack(fill="x", pady=(4, 8), ipady=4)

            tip = tk.Label(self.card_container, text="💡 İpucu: Numaranızı boş bırakırsanız sistem ayarlarından sonradan da açabilirsiniz.", font=("Segoe UI", 8), fg="#71717a", bg="#121214")
            tip.pack(anchor="w")

        elif step == 2:
            self.step_indicator.config(text="Adım 3 / 4 • 🌐 Mobil Web Dashboard & Güvenlik PIN'i")
            self.btn_back.config(state="normal")
            self.btn_next.config(text="İleri ▶", bg="#10b981", fg="#000000")

            ai_bubble = tk.Label(self.card_container, 
                text="🤖 Umutistan AI: Aynı ev/ofis WiFi ağına bağlı cep telefonunuzdan veya tabletinizden\n"
                     "bilgisayar başında olmadan ihaleleri izleyebilir ve teklif ekleyebilirsiniz.\n\n"
                     "Güvenliğiniz için 4 haneli bir mobil PIN kodu belirleyin:",
                font=("Segoe UI", 9), fg="#e4e4e7", bg="#121214", justify="left")
            ai_bubble.pack(anchor="w", pady=(0, 15))

            tk.Label(self.card_container, text="4 Haneli Mobil Giriş PIN Kodu:", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            ent_pin = tk.Entry(self.card_container, textvariable=self.cfg_web_pin, font=("Segoe UI", 12, "bold"), bg="#1c1c20", fg="#10b981", insertbackground="#ffffff", relief="flat", justify="center", width=12)
            ent_pin.pack(anchor="w", pady=(4, 14), ipady=4)

            ip_info_box = tk.Frame(self.card_container, bg="#1e1b4b", padx=12, pady=10, highlightbackground="#4338ca", highlightthickness=1)
            ip_info_box.pack(fill="x", pady=(0, 10))

            tk.Label(ip_info_box, text=f"🌐 Telefonunuzdan Bağlanacağınız Canlı WiFi Adresi:", font=("Segoe UI", 9, "bold"), fg="#a5b4fc", bg="#1e1b4b").pack(anchor="w")
            tk.Label(ip_info_box, text=f"http://{self.local_ip}:5000", font=("Segoe UI", 11, "bold"), fg="#38bdf8", bg="#1e1b4b").pack(anchor="w", pady=(3, 0))

        elif step == 3:
            self.step_indicator.config(text="Adım 4 / 4 • 🎯 İhale Kuralları & VakantieVeilingen Hesabı")
            self.btn_back.config(state="normal")
            self.btn_next.config(text="⚡ SİSTEMİ KUR VE BAŞLAT", bg="#10b981", fg="#000000")

            ai_bubble = tk.Label(self.card_container, 
                text="🤖 Umutistan AI: Neredeyse bitti! Son olarak ihale stratejisi tavsiyeleri:\n\n"
                     "1. Uygulama ilk açıldığında ilk sekmede VakantieVeilingen hesabınıza bir kere\n"
                     "   giriş yapmanız yeterlidir. Giriş çerezleriniz C:\\VakantieVeilingenBot\\Profile\n"
                     "   altında kalıcı olarak saklanır ve tekrar şifre sormaz.\n"
                     "2. İhale varsayılan üst limit ve sniper tetikleme süreleri:",
                font=("Segoe UI", 9), fg="#e4e4e7", bg="#121214", justify="left")
            ai_bubble.pack(anchor="w", pady=(0, 12))

            row = tk.Frame(self.card_container, bg="#121214")
            row.pack(fill="x", pady=4)

            c1 = tk.Frame(row, bg="#121214")
            c1.pack(side="left", fill="x", expand=True, padx=(0, 5))
            tk.Label(c1, text="Varsayılan Üst Limit (€):", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            tk.Entry(c1, textvariable=self.cfg_default_max, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", relief="flat").pack(fill="x", pady=(3, 0), ipady=3)

            c2 = tk.Frame(row, bg="#121214")
            c2.pack(side="left", fill="x", expand=True, padx=5)
            tk.Label(c2, text="Artış (+€):", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            tk.Entry(c2, textvariable=self.cfg_default_step, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", relief="flat").pack(fill="x", pady=(3, 0), ipady=3)

            c3 = tk.Frame(row, bg="#121214")
            c3.pack(side="left", fill="x", expand=True, padx=(5, 0))
            tk.Label(c3, text="Sniper (Son sn):", font=("Segoe UI", 9, "bold"), fg="#a1a1aa", bg="#121214").pack(anchor="w")
            tk.Entry(c3, textvariable=self.cfg_default_sec, font=("Segoe UI", 10), bg="#1c1c20", fg="#ffffff", relief="flat").pack(fill="x", pady=(3, 0), ipady=3)

            final_note = tk.Label(self.card_container, 
                text="✅ Kurulumu tamamladığınızda masaüstünüze 'Umutistan Dolandiricilik Hizmetleri' kısayolu\n"
                     "eklenecek, arka plan servisleri ve ana pencere otomatik olarak başlatılacaktır.",
                font=("Segoe UI", 8.5), fg="#10b981", bg="#121214", justify="left")
            final_note.pack(anchor="w", pady=(15, 0))

    def prev_step(self):
        if self.step > 0:
            self.show_step(self.step - 1)

    def next_step(self):
        if self.step < 3:
            self.show_step(self.step + 1)
        else:
            self.execute_install()

    def quick_install(self):
        self.execute_install()

    def execute_install(self):
        # Progress overlay
        self.clear_card()
        self.btn_back.config(state="disabled")
        self.btn_next.config(state="disabled")
        self.btn_quick.config(state="disabled")

        lbl_status = tk.Label(self.card_container, text="🤖 Umutistan AI Sistemi Yapılandırıyor...", font=("Segoe UI", 11, "bold"), fg="#10b981", bg="#121214")
        lbl_status.pack(pady=(30, 15))

        prog = ttk.Progressbar(self.card_container, orient="horizontal", length=480, mode="determinate")
        prog.pack(pady=10)

        lbl_detail = tk.Label(self.card_container, text="Başlatılıyor...", font=("Segoe UI", 9), fg="#a1a1aa", bg="#121214")
        lbl_detail.pack()

        config_data = {
            "discord_token": self.cfg_discord_token.get().strip(),
            "guild_id": self.cfg_guild_id.get().strip(),
            "whatsapp_enabled": self.cfg_wa_enabled.get(),
            "whatsapp_target_phone": self.cfg_wa_phone.get().strip(),
            "whatsapp_apikey": self.cfg_wa_apikey.get().strip(),
            "web_pin": self.cfg_web_pin.get().strip() or "1234",
            "web_port": self.cfg_web_port.get().strip() or "5000",
            "default_max_bid": self.cfg_default_max.get().strip() or "40.0",
            "default_increment": self.cfg_default_step.get().strip() or "1.0",
            "default_seconds": self.cfg_default_sec.get().strip() or "2",
            "stop_group_on_first_win": "1"
        }

        def update_prog(val, text):
            prog['value'] = val
            lbl_detail.config(text=text)
            self.root.update()
            time.sleep(0.3)

        def worker():
            try:
                perform_installation(config_data, update_prog)
                time.sleep(1.0)
                self.root.destroy()
            except Exception as e:
                import traceback
                messagebox.showerror("Kurulum Hatası", f"Bir hata oluştu:\n{e}\n\n{traceback.format_exc()}")
                self.root.destroy()

        self.root.after(100, worker)


def main():
    root = tk.Tk()
    app = AIOnboardingApp(root)
    root.mainloop()

if __name__ == "__main__":
    main()
