# ============================================================================
# MARTINISTAN VAVABID MOBILE WEB DASHBOARD
# Architected & Developed by: Martinistan / Martiny (@martinistan)
# ============================================================================

MARTINY_AUTHOR = "martinistan"
MARTINY_VERSION = "2.0"

from aiohttp import web
import aiohttp
import asyncio
import json
import os
import sys
import sqlite3
import socket

if sys.platform == 'win32':
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='ignore')
        sys.stderr.reconfigure(encoding='utf-8', errors='ignore')
    except Exception:
        pass

from db_manager import get_config, set_config, get_all_configs
import whatsapp_assistant

PORT = 5000
IPC_DESKTOP_URL = "http://127.0.0.1:58432"

def get_local_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except:
        return "127.0.0.1"

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
    <title>Umutistan Mobil Kontrol & Ayarlar</title>
    <style>
        :root {
            --bg-dark: #09090b;
            --bg-card: #121214;
            --bg-input: #1c1c20;
            --border: #27272a;
            --accent: #10b981;
            --accent-blue: #3b82f6;
            --accent-red: #ef4444;
            --text-muted: #a1a1aa;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }
        body { background: var(--bg-dark); color: #fff; padding: 14px; min-height: 100vh; }
        .header { display: flex; align-items: center; justify-content: space-between; padding-bottom: 14px; border-bottom: 1px solid var(--border); margin-bottom: 16px; }
        .logo { font-size: 18px; font-weight: bold; color: var(--accent); display: flex; align-items: center; gap: 8px; }
        .badge { background: #14532d; color: #4ade80; padding: 4px 8px; border-radius: 99px; font-size: 11px; font-weight: bold; }
        
        .nav-tabs { display: flex; gap: 8px; margin-bottom: 16px; }
        .tab-btn { flex: 1; padding: 10px; background: var(--bg-card); border: 1px solid var(--border); color: var(--text-muted); font-weight: bold; font-size: 13px; border-radius: 8px; cursor: pointer; text-align: center; }
        .tab-btn.active { background: var(--accent); color: #000; border-color: var(--accent); }
        
        .card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; padding: 16px; margin-bottom: 16px; }
        .card-title { font-size: 13px; font-weight: bold; color: var(--text-muted); text-transform: uppercase; margin-bottom: 12px; display: flex; justify-content: space-between; align-items: center; }
        
        .input-group { margin-bottom: 12px; }
        .label { font-size: 12px; font-weight: 600; color: var(--text-muted); margin-bottom: 6px; display: block; }
        input[type="text"], input[type="number"], input[type="password"] {
            width: 100%; background: var(--bg-input); border: 1px solid var(--border); border-radius: 8px;
            color: #fff; padding: 12px; font-size: 14px; outline: none;
        }
        input:focus { border-color: var(--accent); }
        
        .grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 8px; }
        .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
        
        .btn {
            width: 100%; background: var(--accent); color: #000; border: none; border-radius: 8px;
            padding: 13px; font-size: 14px; font-weight: bold; cursor: pointer; text-align: center;
        }
        .btn-blue { background: var(--accent-blue); color: #fff; }
        .btn-red { background: var(--accent-red); color: #fff; }
        .btn-outline { background: transparent; border: 1px solid var(--border); color: #fff; padding: 6px 10px; font-size: 11px; border-radius: 6px; }
        
        .auction-item { background: var(--bg-input); border: 1px solid var(--border); border-radius: 8px; padding: 12px; margin-bottom: 10px; }
        .auc-header { font-size: 14px; font-weight: bold; margin-bottom: 8px; }
        .auc-stats { display: flex; justify-content: space-between; font-size: 13px; color: var(--text-muted); margin-bottom: 10px; }
        .auc-stats span { color: #fff; font-weight: bold; }
        
        #pin-overlay {
            position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: var(--bg-dark);
            display: flex; align-items: center; justify-content: center; z-index: 999; padding: 20px;
        }
        .timer-box { font-size: 20px; font-weight: bold; color: #f59e0b; }
        .ip-bar { background: #1e1b4b; border: 1px solid #4338ca; border-radius: 8px; padding: 8px 12px; font-size: 12px; color: #a5b4fc; margin-bottom: 14px; display: flex; justify-content: space-between; align-items: center; }
    </style>
</head>
<body>

    <!-- CANLI AYAR DEGISIKLIGI UYARI BANDI -->
    <div id="settings-sync-banner" style="display:none; position:sticky; top:0; z-index:9999; background:#ef4444; color:#fff; padding:12px; border-radius:8px; margin-bottom:14px; font-weight:bold; font-size:13px; text-align:center; box-shadow:0 4px 12px rgba(239,68,68,0.5);">
        ⚠️ YENİ AYARLAR GÜNCELLENDİ! Değişiklik yapmadan önce kontrol etmek için sayfayı yenileyin.
        <button onclick="location.reload()" style="margin-left:10px; background:#fff; color:#ef4444; border:none; padding:4px 12px; border-radius:6px; font-weight:bold; cursor:pointer;">↻ Yenile</button>
    </div>

    <!-- PIN GUVENLIK EKRANI -->
    <div id="pin-overlay">
        <div class="card" style="width: 100%; max-width: 340px; text-align: center;">
            <div class="logo" style="justify-content: center; margin-bottom: 16px;">◈ UMUTİSTAN GİRİŞ</div>
            <p style="font-size: 13px; color: var(--text-muted); margin-bottom: 16px;">Devam etmek için 4 haneli PIN kodunuzu girin</p>
            <input type="password" id="pin-input" maxlength="4" placeholder="••••" style="text-align: center; font-size: 24px; letter-spacing: 8px; margin-bottom: 16px;">
            <button class="btn" onclick="verifyPin()">GİRİŞ YAP</button>
            <p id="pin-error" style="color: var(--accent-red); font-size: 12px; margin-top: 10px; display: none;">Hatalı PIN Kodu!</p>
        </div>
    </div>

    <!-- ANA HEADER -->
    <div class="header">
        <div class="logo">◈ UMUTİSTAN BIEDER</div>
        <div class="badge" id="conn-status">● CANLI BAĞLANTI</div>
    </div>

    <div class="ip-bar">
        <span>🌐 <b>Yerel IP:</b> http://###LOCAL_IP###:###PORT###</span>
        <span style="font-size: 10px; background: #3730a3; padding: 2px 6px; border-radius: 4px;">WiFi Erişimi</span>
    </div>

    <!-- TAB BUTONLARI -->
    <div class="nav-tabs">
        <div class="tab-btn active" id="tab-btn-auctions" onclick="switchTab('auctions')">⚡ İhaleler</div>
        <div class="tab-btn" id="tab-btn-settings" onclick="switchTab('settings')">⚙️ Sistem & WhatsApp</div>
    </div>

    <!-- TAB 1: IHALELER -->
    <div id="tab-auctions">
        <div class="card">
            <div class="card-title">Yeni Açık Artırma Ekle</div>
            <div class="input-group">
                <label class="label">VakantieVeilingen Linki</label>
                <input type="text" id="url" placeholder="https://www.vakantieveilingen.be/...">
            </div>
            <div class="grid-3" style="margin-bottom: 14px;">
                <div>
                    <label class="label">Maks. (€)</label>
                    <input type="number" id="max" value="40">
                </div>
                <div>
                    <label class="label">Artış (+€)</label>
                    <input type="number" id="step" value="1">
                </div>
                <div>
                    <label class="label">Son (sn)</label>
                    <input type="number" id="sec" value="2">
                </div>
            </div>
            <button class="btn" onclick="addAuction()">▶ İZLEMEYİ BAŞLAT</button>
        </div>

        <div class="card">
            <div class="card-title">
                <span>Canlı Takipteki İhaleler</span>
                <button class="btn-outline" onclick="loadAuctions()">↻ Yenile</button>
            </div>
            <div id="auction-list">
                <p style="font-size: 13px; color: var(--text-muted); text-align: center; padding: 12px;">Yükleniyor...</p>
            </div>
        </div>
    </div>

    <!-- TAB 2: AYARLAR (DISCORD + WHATSAPP + PIN) -->
    <div id="tab-settings" style="display: none;">
        <div class="card">
            <div class="card-title">📱 WhatsApp Bildirim Ayarları</div>
            <div class="input-group">
                <label class="label">WhatsApp Bildirimleri</label>
                <select id="cfg-wa-enabled" style="width:100%; background:var(--bg-input); color:#fff; border:1px solid var(--border); padding:10px; border-radius:8px;">
                    <option value="1">Açık (Kazanıldığında mesaj at)</option>
                    <option value="0">Kapalı</option>
                </select>
            </div>
            <div class="input-group">
                <label class="label">Hedef Telefon Numarası (+90... veya +31...)</label>
                <input type="text" id="cfg-wa-phone" placeholder="+905321234567">
            </div>
            <div class="input-group">
                <label class="label">CallMeBot API Key (Ücretsiz WhatsApp Key)</label>
                <input type="text" id="cfg-wa-apikey" placeholder="Örn: 987654">
            </div>
            <button class="btn btn-blue" style="margin-bottom: 8px;" onclick="testWhatsApp()">💬 WhatsApp Test Mesajı Gönder</button>
            <p id="wa-test-result" style="font-size: 12px; color: var(--text-muted);"></p>
        </div>

        <div class="card">
            <div class="card-title">🤖 Discord & Güvenlik Ayarları</div>
            <div class="input-group">
                <label class="label">Discord Bot Token</label>
                <input type="password" id="cfg-discord-token" placeholder="Bot Token">
            </div>
            <div class="input-group">
                <label class="label">Discord Sunucu (Guild) ID</label>
                <input type="text" id="cfg-discord-guild" placeholder="Sunucu ID">
            </div>
            <div class="grid-2">
                <div class="input-group">
                    <label class="label">Web PIN Kodu</label>
                    <input type="text" id="cfg-web-pin" maxlength="4" placeholder="1234">
                </div>
                <div class="input-group">
                    <label class="label">Varsayılan Limit (€)</label>
                    <input type="number" id="cfg-default-max" value="40">
                </div>
            </div>
            <button class="btn" onclick="saveAllSettings()">💾 AYARLARI KAYDET</button>
            <p id="settings-save-result" style="font-size: 12px; color: var(--accent); margin-top: 8px; text-align: center; display: none;">✅ Ayarlar kaydedildi!</p>
        </div>
    </div>

    <script>
        const correctPin = "###PIN_PLACEHOLDER###";
        
        function verifyPin() {
            const val = document.getElementById("pin-input").value;
            if (val === correctPin || correctPin === "") {
                document.getElementById("pin-overlay").style.display = "none";
                sessionStorage.setItem("auth", "true");
                startPolling();
                loadSettings();
            } else {
                document.getElementById("pin-error").style.display = "block";
            }
        }

        if (sessionStorage.getItem("auth") === "true") {
            document.getElementById("pin-overlay").style.display = "none";
            startPolling();
            loadSettings();
        }

        function switchTab(tab) {
            document.getElementById("tab-auctions").style.display = tab === 'auctions' ? 'block' : 'none';
            document.getElementById("tab-settings").style.display = tab === 'settings' ? 'block' : 'none';
            document.getElementById("tab-btn-auctions").className = 'tab-btn ' + (tab === 'auctions' ? 'active' : '');
            document.getElementById("tab-btn-settings").className = 'tab-btn ' + (tab === 'settings' ? 'active' : '');
            if (tab === 'settings') loadSettings();
        }

        async function addAuction() {
            const url = document.getElementById("url").value;
            const max = document.getElementById("max").value;
            const step = document.getElementById("step").value;
            const sec = document.getElementById("sec").value;

            if (!url) return alert("Lütfen ihale linkini yapıştırın.");

            const res = await fetch(`/api/add?url=${encodeURIComponent(url)}&max=${max}&step=${step}&sec=${sec}`);
            if (res.ok) {
                alert("İhale takibe alındı!");
                document.getElementById("url").value = "";
                loadAuctions();
            } else {
                alert("Ekleme başarısız.");
            }
        }

        async function loadAuctions() {
            try {
                const res = await fetch("/api/status");
                const items = await res.json();
                const container = document.getElementById("auction-list");

                if (!items || items.length === 0) {
                    container.innerHTML = '<p style="font-size: 13px; color: var(--text-muted); text-align: center; padding: 12px;">Aktif açık sekme yok.</p>';
                    return;
                }

                container.innerHTML = items.map(x => `
                    <div class="auction-item">
                        <div class="auc-header">${x.title || 'Açık Artırma'}</div>
                        <div class="auc-stats">
                            <div>Sitedeki: <span>${x.currentBid || 0} €</span></div>
                            <div class="timer-box">⏱️ ${x.timerText || '—'}</div>
                            <div>Limit: <span>${x.max || 0} €</span></div>
                        </div>
                        <div style="display:flex; justify-content:space-between; align-items:center;">
                            <span style="font-size:12px; font-weight:bold; color:${x.armed ? '#10b981' : '#ef4444'}">
                                ${x.armed ? '● İZLENİYOR' : '⏸️ DURDURULDU'}
                            </span>
                            <button class="btn-outline" style="background:#dc2626; border:none;" onclick="stopAuction('${encodeURIComponent(x.url)}')">Durdur</button>
                        </div>
                    </div>
                `).join("");
            } catch (e) {
                console.error(e);
            }
        }

        async function stopAuction(url) {
            await fetch(`/api/stop?url=${url}`);
            loadAuctions();
        }

        async function loadSettings() {
            try {
                const res = await fetch("/api/settings");
                const cfg = await res.json();
                if (cfg) {
                    if (cfg.whatsapp_enabled !== undefined) document.getElementById("cfg-wa-enabled").value = cfg.whatsapp_enabled;
                    if (cfg.whatsapp_target_phone) document.getElementById("cfg-wa-phone").value = cfg.whatsapp_target_phone;
                    if (cfg.whatsapp_apikey) document.getElementById("cfg-wa-apikey").value = cfg.whatsapp_apikey;
                    if (cfg.discord_token) document.getElementById("cfg-discord-token").value = cfg.discord_token;
                    if (cfg.guild_id) document.getElementById("cfg-discord-guild").value = cfg.guild_id;
                    if (cfg.web_pin) document.getElementById("cfg-web-pin").value = cfg.web_pin;
                    if (cfg.default_max_bid) document.getElementById("cfg-default-max").value = cfg.default_max_bid;
                }
            } catch(e) {}
        }

        async function saveAllSettings() {
            const data = {
                whatsapp_enabled: document.getElementById("cfg-wa-enabled").value,
                whatsapp_target_phone: document.getElementById("cfg-wa-phone").value,
                whatsapp_apikey: document.getElementById("cfg-wa-apikey").value,
                discord_token: document.getElementById("cfg-discord-token").value,
                guild_id: document.getElementById("cfg-discord-guild").value,
                web_pin: document.getElementById("cfg-web-pin").value,
                default_max_bid: document.getElementById("cfg-default-max").value
            };
            const res = await fetch("/api/settings", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(data)
            });
            if (res.ok) {
                const el = document.getElementById("settings-save-result");
                el.style.display = "block";
                setTimeout(() => el.style.display = "none", 3000);
            }
        }

        async function testWhatsApp() {
            const phone = document.getElementById("cfg-wa-phone").value;
            const apikey = document.getElementById("cfg-wa-apikey").value;
            const resEl = document.getElementById("wa-test-result");
            resEl.innerText = "Gönderiliyor...";
            try {
                const res = await fetch(`/api/test_whatsapp?phone=${encodeURIComponent(phone)}&apikey=${encodeURIComponent(apikey)}`);
                const data = await res.json();
                resEl.innerText = (data.ok ? "✅ " : "❌ ") + (data.message || "Bilinmeyen durum");
                resEl.style.color = data.ok ? "#4ade80" : "#ef4444";
            } catch(e) {
                resEl.innerText = "Hata: " + e;
            }
        let currentSettingsVersion = null;
        async function checkSettingsVersion() {
            try {
                const res = await fetch("/api/settings_version");
                const data = await res.json();
                if (currentSettingsVersion === null) {
                    currentSettingsVersion = data.version;
                } else if (data.version && data.version !== currentSettingsVersion) {
                    document.getElementById("settings-sync-banner").style.display = "block";
                }
            } catch(e) {}
        }

        function startPolling() {
            loadAuctions();
            setInterval(loadAuctions, 1500);
            checkSettingsVersion();
            setInterval(checkSettingsVersion, 2500);
        }
    </script>
</body>
</html>
"""

def normalize_url(url):
    if not url: return "https://www.vakantieveilingen.be/"
    url = url.strip()
    if url.startswith("/"):
        return f"https://www.vakantieveilingen.be{url}"
    if not url.startswith("http://") and not url.startswith("https://"):
        return f"https://www.vakantieveilingen.be/{url}"
    return url

async def index(request):
    pin = get_config("web_pin", "1234")
    local_ip = get_local_ip()
    html = HTML_TEMPLATE.replace("###PIN_PLACEHOLDER###", pin)
    html = html.replace("###LOCAL_IP###", local_ip)
    html = html.replace("###PORT###", str(PORT))
    return web.Response(text=html, content_type="text/html")

async def api_status(request):
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/status", timeout=2) as resp:
                if resp.status == 200:
                    data = await resp.json()
                    return web.json_response(data)
    except:
        pass
    return web.json_response([])

async def api_add(request):
    raw_url = request.query.get("url", "")
    url = normalize_url(raw_url)
    max_val = request.query.get("max", "40")
    step = request.query.get("step", "1")
    sec = request.query.get("sec", "2")

    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/add?url={url}&max={max_val}&step={step}&sec={sec}", timeout=3) as resp:
                return web.json_response({"ok": resp.status == 200})
    except Exception as e:
        return web.json_response({"ok": False, "error": str(e)}, status=500)

async def api_stop(request):
    url = request.query.get("url", "")
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/stop?url={url}", timeout=2) as resp:
                return web.json_response({"ok": resp.status == 200})
    except:
        pass
    return web.json_response({"ok": False})

async def api_get_settings(request):
    configs = get_all_configs()
    return web.json_response(configs)

async def api_settings_version(request):
    ver = get_config("settings_version", "1")
    return web.json_response({"version": ver})

async def api_post_settings(request):
    try:
        data = await request.json()
        for k, v in data.items():
            set_config(k, str(v))
        # settings.json dosyasini da guncelle
        try:
            cfg_path = r"C:\VakantieVeilingenBot\settings.json"
            current = {}
            if os.path.exists(cfg_path):
                with open(cfg_path, "r", encoding="utf-8") as f:
                    current = json.load(f)
            current.update(data)
            current["settings_version"] = str(int(asyncio.get_event_loop().time()))
            with open(cfg_path, "w", encoding="utf-8") as f:
                json.dump(current, f, indent=4)
        except:
            pass
        return web.json_response({"ok": True})
    except Exception as e:
        return web.json_response({"ok": False, "error": str(e)}, status=500)

async def api_test_whatsapp(request):
    phone = request.query.get("phone")
    apikey = request.query.get("apikey")
    ok, msg = whatsapp_assistant.test_whatsapp_alert(phone, apikey)
    return web.json_response({"ok": ok, "message": msg})

async def api_set_limit(request):
    try:
        url = request.query.get("url", "")
        max_val = request.query.get("max", "32")
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/set_limit?url={url}&max={max_val}", timeout=2) as resp:
                return web.json_response({"ok": resp.status == 200})
    except:
        pass
    return web.json_response({"ok": False})

async def api_shutdown(request):
    cmd = request.query.get("cmd", "")
    reason = request.query.get("reason", "Web Panel uzerinden tetiklendi")
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/shutdown?cmd={cmd}&reason={reason}", timeout=2) as resp:
                return web.json_response({"ok": resp.status == 200})
    except:
        pass
    return web.json_response({"ok": False})

def create_app():
    app = web.Application()
    app.router.add_get("/", index)
    app.router.add_get("/api/status", api_status)
    app.router.add_get("/api/add", api_add)
    app.router.add_get("/api/stop", api_stop)
    app.router.add_get("/api/set_limit", api_set_limit)
    app.router.add_get("/api/shutdown", api_shutdown)
    app.router.add_get("/api/settings", api_get_settings)
    app.router.add_get("/api/settings_version", api_settings_version)
    app.router.add_post("/api/settings", api_post_settings)
    app.router.add_get("/api/test_whatsapp", api_test_whatsapp)
    return app

def run_web():
    try:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        app = create_app()
        local_ip = get_local_ip()
        print(f"[OK] Mobil Web Dashboard: http://{local_ip}:{PORT}")
        web.run_app(app, host="0.0.0.0", port=PORT, handle_signals=False, loop=loop)
    except Exception as e:
        print(f"[HATA] run_web hatasi: {e}")

if __name__ == "__main__":
    run_web()
