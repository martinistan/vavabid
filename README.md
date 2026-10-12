# 🦝 Umutistan Dolandırıcılık Hizmetleri (vavabid)
### VakantieVeilingen Autonomous Sniper & Auction Automation Suite

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6.svg?logo=windows)](https://microsoft.com)
[![Engine](https://img.shields.io/badge/Engine-C%23%20.NET%20%2B%20WebView2-512BD4.svg?logo=dotnet)](https://github.com/martinistan/vavabid)
[![Backend](https://img.shields.io/badge/Backend-Python%20%7C%20Discord%20%7C%20WhatsApp-3776AB.svg?logo=python)](https://github.com/martinistan/vavabid)
[![License](https://img.shields.io/badge/License-Open%20Source-10B981.svg)](LICENSE)

Umutistan Dolandırıcılık Hizmetleri (vavabid), [VakantieVeilingen](https://www.vakantieveilingen.nl) açık artırmaları için geliştirilmiş yüksek frekanslı, çok sekmeli otonom ihale takip ve son saniye teklif (sniper) otomasyonudur.

> 🤖 **AI / LLM Asistanları İçin Özel Kılavuz**: Bu projeyi bir yapay zekaya (ChatGPT, Claude, Cursor, Copilot vb.) tanıtmak ve kod üzerinde geliştirme yaptırmak istiyorsanız lütfen [`README.AI.md`](README.AI.md) dosyasını inceleyin.

---

## ✨ Temel Özellikler

- 🎯 **Milisaniye Hassasiyetli Sniper**: İhalenin bitmesine belirlenen saniye kala (varsayılan: 1 saniye) otomatik +1 € artırımlı teklif verir.
- 🌐 **Chrome Tarzı Modern Sekmeli Arayüz**: İleri (`→`), Geri (`←`), Yenile (`⟳`), `F5` / `Ctrl+R` kısayolları ve kapsül Omnibox adres çubuğu (`🔒`).
- 🛑 **Limit Aşımı Koruması & Otonom Durdurma**: İhale fiyatı belirlediğiniz azami limiti (varsayılan: 32 €) aşarsa anında Discord ve WhatsApp uyarısı gönderir; limit artırılmazsa teklif vermeden kendini güvenle devre dışı bırakır (`Disarm`).
- 💤 **Otonom Bilgisayar Kapatma (PC Auto-Shutdown)**: İhale bittiğinde veya limit aşıldığında bilgisayarı 60 saniyelik geri sayım ile otomatik kapatır (`shutdown.exe /s /t 60`). Arayüzden veya Discord'dan tek tıkla iptal edilebilir (`shutdown.exe /a`).
- 🤖 **Discord Bot Entegrasyonu**: Discord üzerinden `/ihale`, `/limit`, `/durdur`, `/kapat_iptal` gibi komutlarla botu uzaktan yönetin, anlık ekran görüntüsü alın ve durum kartlarını takip edin.
- 📱 **Doğrudan WhatsApp Bildirimleri**: CallMeBot API köprüsü ile ihale kazanıldığında veya limit aşıldığında telefonunuza anlık WhatsApp mesajı iletilir.
- 📊 **Uzaktan Web Kontrol Paneli**: Port 5000 üzerinden çalışan Flask Web Dashboard ile mobil cihazınızdan veya yerel ağınızdan ihaleleri izleyin ve limitleri güncelleyin.
- 📦 **Tek Tıkla Kurulum & Güncelleme**: `Umutistan_Tek_Tikla_Kurulum.exe` ile `C:\VakantieVeilingenBot\` dizinine tüm bağımlılıkları tek seferde kurar, kullanıcı verilerini (`database.db` ve `settings.json`) asla ezmez.

---

## 🚀 Hızlı Başlangıç & Kurulum

### 1. Tek Tıkla Kurulum (Önerilen)
1. [Releases](../../releases) bölümünden veya masaüstünden `Umutistan_Tek_Tikla_Kurulum.exe` dosyasını çalıştırın.
2. Açılan kurulum sihirbazında Discord Bot Token, WhatsApp API anahtarı ve web PIN bilgilerinizi girin (veya Hızlı Kurulum'a tıklayın).
3. Kurulum tamamlandığında masaüstünüze kısayol oluşturulacak ve bot otomatik olarak başlayacaktır.

### 2. Kaynak Koddan Derleme

#### Gereksinimler:
- Windows 10 veya 11
- .NET Framework 4.8 Runtime / SDK (`csc.exe`)
- Python 3.10+ (opsiyonel, harici servisleri Python ile çalıştırmak için)
- Microsoft Edge WebView2 Runtime

#### Derleme Komutları:
```cmd
:: 1. Ana Uygulamayı Derleyin (C# 5 syntax uyumlu):
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app-logo.ico /win32manifest:app.manifest /out:UmutistanDolandiricilikHizmetleri.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll,Microsoft.Web.WebView2.Core.dll,Microsoft.Web.WebView2.WinForms.dll Program.cs MainForm.cs

:: 2. Tek Tıkla Kurulum Paketini Derleyin:
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app-logo.ico /resource:payload.zip /out:Umutistan_Tek_Tikla_Kurulum.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll InstallerApp.cs
```

---

## ⚙️ Yapılandırma (`settings.json`)

`C:\VakantieVeilingenBot\settings.json` dosyası yapısı:
```json
{
  "discord_token": "DISCORD_BOT_TOKEN",
  "guild_id": "DISCORD_SUNUCU_ID",
  "whatsapp_enabled": "1",
  "whatsapp_target_phone": "+905xxxxxxxxx",
  "whatsapp_apikey": "CALLMEBOT_API_KEY",
  "web_pin": "1234",
  "web_port": "5000",
  "default_max_bid": "32",
  "default_increment": "1",
  "default_seconds": "1",
  "stop_group_on_first_win": "1"
}
```

---

## 🎮 Kısayollar & Kontroller

- `F5` veya `Ctrl + R`: Aktif açık artırma sekmesini yeniden yükler.
- `←` / `→`: Tarayıcı geçmişinde geri ve ileri gider.
- `🔒 Adres Çubuğu`: URL girip Enter'a basarak doğrudan ihaleye gidin.
- `+/- Butonları & Sayı Girişi`: Maksimum limitinizi doğrudan klavyeyle yazın veya butonlara basılı tutarak hızlıca artırıp azaltın.

---

## 📜 Lisans & Sorumluluk Reddi

Bu yazılım yalnızca eğitim ve kişisel otomasyon amacıyla geliştirilmiştir. Kullanımdan doğabilecek hesap yaptırımları ve maddi sorumluluklar kullanıcıya aittir.
