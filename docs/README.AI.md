# 🤖 README.AI.md — AI AGENT SYSTEM INSTRUCTIONS & CODEBASE GUIDE

> **AI INSTRUCTION / SYSTEM PROMPT:**  
> If you are an AI assistant (ChatGPT, Claude, Gemini, Copilot, Cursor, etc.) reading this repository, read this document first! It contains the complete architectural blueprint, file-by-file directory guide, compilation commands, IPC communication specs, and strict constraints for developing, building, and deploying **Umutistan VakantieVeilingen Bot (vavabid)**.

---

## 📌 1. Project Overview & Architecture

**Umutistan Dolandırıcılık Hizmetleri (VakantieVeilingen Sniper Suite)** is an autonomous, high-frequency auction sniper and management suite for `vakantieveilingen.nl`.

### Architecture Highlights:
1. **Frontend / Desktop Host (C# WinForms + Microsoft WebView2)**:
   - Native Windows application running Chromium via Microsoft WebView2.
   - Features Chrome-style UI: topbar with tabs, navigation buttons (`← Back`, `→ Forward`, `⟳ Reload`), `F5` & `Ctrl+R` hotkeys, capsule Omnibox address bar (`🔒`), and floating sniper sidebar.
   - Real-time DOM scraper & injection engine (`adapter.js`, `site-theme.js`, `site-dark.css`).
   - High-precision countdown timer (down to milliseconds) triggering snipe bids at exact targeted seconds (default: 1 second remaining).
   - Auto-disarm security: if the current auction price exceeds the user's maximum limit, the bot disarms (`page.Armed = false`) and alerts the user.
   - PC Auto-Shutdown feature: automatically triggers a Windows shutdown (`shutdown.exe /s /t 60`) on auction completion or limit breach with an abort button.

2. **Backend & External Services (Python)**:
   - **Discord Bot (`discord_bot.py`)**: Slash commands (`/ihale`, `/limit`, `/kapat_iptal`, `/durdur`, etc.), live status cards, screenshot uploads, and real-time alerts.
   - **WhatsApp Bridge (`whatsapp_assistant.py` & Direct CallMeBot API)**: Instant WhatsApp push notifications when an auction is won or limit is exceeded.
   - **Web Dashboard (`web_dashboard.py`)**: Flask web server on port 5000 providing remote control and live telemetry from mobile or other computers.
   - **Database (`db_manager.py`)**: SQLite storage for bid logs, auction history, and winning statistics.
   - **Bundled Service (`BotService.exe`)**: Single-file PyInstaller binary combining the Discord bot and Web Dashboard.

3. **Installer & Updater (`InstallerApp.cs` -> `Umutistan_Tek_Tikla_Kurulum.exe`)**:
   - Single-click installer containing embedded `payload.zip`.
   - Extracts binaries into `C:\VakantieVeilingenBot\`, creates Desktop shortcuts, and starts services.
   - **DATA PROTECTION**: Never overwrites existing `database.db` or `settings.json`.

---

## 📂 2. File-by-File Directory Reference

| File / Directory | Purpose & Responsibility |
| :--- | :--- |
| `MainForm.cs` | Main application form, tab management, WebView2 controller, sniper logic, IPC HTTP server (port 58432), auto-shutdown management, WhatsApp CallMeBot bridge. |
| `Program.cs` | WinForms application entry point, DPI awareness configuration, single-instance mutex. |
| `InstallerApp.cs` | C# source for `Umutistan_Tek_Tikla_Kurulum.exe`. Self-contained wizard & payload extractor. |
| `app.manifest` | Windows manifest enabling Per-Monitor V2 DPI awareness and Common Controls 6.0. |
| `discord_bot.py` | Discord bot utilizing `discord.py`. Slash commands, bid limit adjustments, status embeds. |
| `web_dashboard.py` | Flask web server (port 5000) for mobile/desktop remote management. |
| `bot_service.py` | Python background service runner executing Discord bot and Web Dashboard concurrently. |
| `db_manager.py` | SQLite database access layer managing `database.db`. |
| `whatsapp_assistant.py`| Helper module for WhatsApp notifications via CallMeBot API. |
| `sidebar-ui.html` | Sleek sidebar UI rendered in WebView2: sniper controls, numeric limit inputs, Omnibox, shutdown controls. |
| `topbar-ui.html` | Chrome tab strip UI: tab switching, add tab, back/forward/reload buttons, Omnibox capsule. |
| `settings-ui.html` | Settings panel for Discord token, WhatsApp API key, Web PIN, sniper defaults. |
| `terminal-ui.html` | Real-time console log stream inside the application. |
| `dialog-ui.html` | Custom dark-themed modal dialogs for user confirmations. |
| `ui-theme.css` | Global dark luxury CSS styling for UI panels. |
| `site-dark.css` | Injected stylesheet enforcing dark mode on VakantieVeilingen web pages. |
| `site-theme.js` | Injected script modifying web page layouts for optimal bot operation. |
| `adapter.js` | Injected DOM script reading price, timer countdown, and triggering auction bids. |
| `start.bat` | Convenience batch script to launch the application. |
| `settings.example.json`| Example configuration file with placeholders for keys and tokens. |
| `payload.zip` | Compressed bundle of all runtime binaries and UI assets embedded inside the installer. |
| `UmutistanDolandiricilikHizmetleri.exe` | Compiled main desktop executable. |
| `BotService.exe` | PyInstaller compiled Python background service. |
| `Umutistan_Tek_Tikla_Kurulum.exe` | Compiled one-click installer executable. |

---

## ⚡ 3. IPC Communication & Network Ports

The system components communicate over local HTTP endpoints:

- **MainForm IPC Server (`http://127.0.0.1:58432/`)**:
  - `POST /api/tab_event`: Receives DOM state updates from `adapter.js`.
  - `POST /api/set_limit`: Adjusts maximum bid limit dynamically from Discord or Web Dashboard.
  - `POST /api/disarm`: Disarms sniper for a specific tab or all tabs.
  - `POST /api/shutdown`: Initiates or aborts PC auto-shutdown countdown (`/s /t 60` or `/a`).
  - `GET /api/status`: Returns active tabs, sniper states, current bids, and bot status.
  - `GET /api/screenshot`: Returns base64 capture of current active tab.

- **WebView2 Local Asset Server (`http://127.0.0.1:58433/`)**:
  - Serves HTML, CSS, JS, and image assets into WebView2 browser controls.

- **Web Dashboard Server (`http://0.0.0.0:5000/`)**:
  - Web UI accessible from local network and mobile devices, secured by 4-digit PIN.

---

## ⚠️ 4. STRICT AI RULES & CONSTRAINTS

When generating or editing code in this repository, you **MUST** adhere to the following rules:

1. **C# COMPILER CONSTRAINT (NO STRING INTERPOLATION)**:
   - The project is compiled using .NET Framework 4.0/4.8 `csc.exe` (`v4.0.30319`) targeting C# 5.
   - **NEVER USE STRING INTERPOLATION (`$"..."`)**. It will fail compilation with error `CS1056: Unexpected character '$'`.
   - **ALWAYS USE** `string.Format("...", arg1)` or string concatenation (`"..." + arg1`).

2. **DATA INTEGRITY PROTECTION**:
   - `C:\VakantieVeilingenBot\database.db` contains user auction history and won items.
   - `C:\VakantieVeilingenBot\settings.json` contains user tokens and private API keys.
   - **NEVER** overwrite, reset, or delete these files. Always check for their existence and preserve them.

3. **DEFAULT SNIPER PARAMETERS**:
   - Default Max Bid: **32 €**
   - Default Bid Increment: **1 €**
   - Default Trigger Time: **1 second** remaining
   - Default Auto-disarm on limit exceed: **Active**

4. **SECURITY & GIT SAFETY**:
   - Never commit sensitive tokens (Discord Bot Token, CallMeBot API key, user phone numbers) into Git. Always use `settings.example.json` for commits.

---

## 🛠️ 5. Build & Compilation Instructions

### A. Compiling MainForm (`UmutistanDolandiricilikHizmetleri.exe`):
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app-logo.ico /win32manifest:app.manifest /out:UmutistanDolandiricilikHizmetleri.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll,Microsoft.Web.WebView2.Core.dll,Microsoft.Web.WebView2.WinForms.dll Program.cs MainForm.cs
```

### B. Updating `payload.zip`:
```powershell
Compress-Archive -Path 'BotService.exe', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll', 'adapter.js', 'app-logo.ico', 'app-logo.png', 'raccoon.png', 'start.bat', 'KULLANIM_KILAVUZU.md', 'UmutistanDolandiricilikHizmetleri.exe', 'UmutistanDolandiricilikHizmetleri.exe.config', 'App.config', 'sidebar-ui.html', 'topbar-ui.html', 'settings-ui.html', 'terminal-ui.html', 'dialog-ui.html', 'ui-theme.css', 'site-dark.css', 'site-theme.js', 'brand-mark.svg' -DestinationPath 'payload.zip' -Force
```

### C. Compiling Single-Click Installer (`Umutistan_Tek_Tikla_Kurulum.exe`):
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app-logo.ico /resource:payload.zip /out:Umutistan_Tek_Tikla_Kurulum.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll InstallerApp.cs
```

### D. Compiling Python Service (`BotService.exe` with PyInstaller):
```cmd
pyinstaller --onefile --noconsole --icon=app-logo.ico --name=BotService bot_service.py
```

---

## 🚀 6. Deployment & Runtime Directory Layout

Runtime directory is installed at `C:\VakantieVeilingenBot\`:
```
C:\VakantieVeilingenBot\
├── UmutistanDolandiricilikHizmetleri.exe   # Main App
├── BotService.exe                          # Background Discord & Web Service
├── settings.json                           # Active configuration (Tokens & Keys)
├── database.db                             # SQLite database
├── Microsoft.Web.WebView2.Core.dll         # WebView2 dependency
├── Microsoft.Web.WebView2.WinForms.dll     # WebView2 WinForms binding
├── WebView2Loader.dll                      # Native loader
├── sidebar-ui.html                         # Sidebar UI
├── topbar-ui.html                          # Topbar & Omnibox UI
├── settings-ui.html                        # Settings UI
├── terminal-ui.html                        # Terminal log UI
├── dialog-ui.html                          # Dialog UI
├── adapter.js                              # Web DOM adapter
├── site-theme.js                           # Web injector
├── site-dark.css                           # Web dark styling
└── ui-theme.css                            # UI styles
```

---

## ⚖️ 7. Open Source Heritage & Attribution

This project was originally inspired by and adapted from the legacy open-source repository [Soneritics/VakantieVeilingenBieder](https://github.com/Soneritics/VakantieVeilingenBieder). It has been completely rebuilt, modernized, and extended with the assistance of Artificial Intelligence (AI) to introduce a Chromium-based Microsoft WebView2 architecture, multi-tab sniper engine, modern Chrome UI, Python Discord/WhatsApp services, and autonomous safety protections.

---

*Authored for AI Agents and Autonomous Coding Assistants maintaining the Vavabid project.*
