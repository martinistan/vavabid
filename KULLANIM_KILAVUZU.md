# ◈ UMUTİSTAN DOLANDIRICILIK HİZMETLERİ
## VakantieVeilingen Otonom İhale & Çok Kanallı Yönetim Sistemi
### Kullanım Kılavuzu & Sistem Dokümantasyonu

---

### 🚀 HIZLI BAŞLANGIÇ

1. **Çift Tıklayın:** `start.bat` dosyasına çift tıklayın.
2. **İlk Kurulum (Sadece 1 Seferlik):**
   - Karşınıza çıkan şık sihirbazda Discord Token ve Sunucu ID'nizi girebilirsiniz *(İstemezseniz boş bırakıp 'KURULUMU TAMAMLA' diyebilirsiniz; sistem Discord olmadan da %100 çalışır)*.
   - Web Dashboard için 4 haneli PIN kodunuzu belirleyin *(Varsayılan: `1234`)*.
3. **Masaüstü Ekranı Açılır:**
   - Açılan saf mat siyah pencerede VakantieVeilingen hesabınıza bir kez giriş yapın.
   - Çerezler ve oturum `C:\VakantieVeilingenBot\Profile` içinde kalıcı saklanır; sonraki açılışlarda tekrar giriş yapmanız gerekmez!

---

### 💻 MASAÜSTÜ KULLANIMI

* **Sekme Sistemi (Chrome/Edge Tarzı):**
  - Üst kısımdaki `+` butonuna basarak istediğiniz kadar açık artırmayı aynı anda takip edebilirsiniz.
  - Çok fazla sekme açtığınızda sekmeler küçülüp ezilmez; fare tekerleğiyle yatay kaydırabilirsiniz.
* **Sekmeye Özel Limitler:**
  - `Üst Limit (€)`: İhaleye verebileceğiniz maksimum tutar (Örn: 40 €).
  - `Artış (+€)`: Mevcut teklifin üstüne eklenecek artış (Standart: 1 €).
  - `Tetikleme (sn)`: Teklifin verileceği son saniye eşiği (Standart: 2 sn).
* **Akıllı Teklif Mantığı (Para Kaybını Önleme):**
  - Sitede fiyat 11 € ise ve limitiniz 40 € ise sistem asla 40 € basmaz!
  - `11 € + 1 € = 12 €` teklif verir. Rakipler artırırsa adım adım takip eder, belirlediğiniz üst sınırı asla geçmez.
* **Kazanç Algılama:**
  - İhale kazanıldığı an motor durur, sesli uyarı verir, 4K ekran görüntüsü çeker ve Discord/WhatsApp'a gönderir.

---

### 📱 TELEFONDAN KONTROL (MOBİL WEB PANELİ)

* Masaüstü arayüzündeki **`📱 Mobil QR`** butonuna basın.
* Telefon kameranızla ekrandaki QR kodu okutun veya aynı WiFi üzerindeyken tarayıcınıza gösterilen adresi girin:
  `http://192.168.x.x:5000`
* Belirlediğiniz 4 haneli PIN'i girin *(Varsayılan: 1234)*.
* **Telefondan Yapabilecekleriniz:**
  - Canlı geri sayımı ve fiyatı milisaniyelik izleme.
  - Yeni ihale linki yapıştırıp uzaktan başlatma.
  - Tek dokunuşla ihaleyi durdurma.

---

### 🤖 DİSCORD BOTU & GİZLİ KANAL YÖNETİMİ

Discord Bot Token'ınızı tanımladığınızda bot sunucunuza bağlanır ve otomatik olarak:
1. `🔒 VAKANTIE-BOT YÖNETİM` kategorisi açar.
2. `@everyone` rolünün **tüm görme ve okuma izinlerini kapatır** (kanallar diğer üyelere tamamen görünmezdir).
3. 6 özel kanal kurar:
   - `#aktif-ihaleler` : Canlı izlenen ürünler ve butonlar (`[+5€ Artır]`, `[Durdur]`, `[SS Al]`).
   - `#kazanılanlar-arşiv` : Kazanılan ihaleler, final fiyat ve 4K ekran görüntüsü.
   - `#log-teklifler` : Yapılan her teklifin detayları.
   - `#log-hatalar` : Sistem uyarıları.

#### ⚡ Slash Komutları:
* `/ekle url max artis sonsn` : İhaleyi uzaktan başlatır.
* `/liste` : İzlenen ihaleleri listeler.
* `/ss` : O anki canlı ekran görüntüsünü çeker (Discord CDN'e aktarıldıktan sonra yerel diskten anında silinir, sıfır çöp dosya).
* `/durdur` : Takibi durdurur.
* `/durum` : Sistem ve donanım durumunu gösterir.

---

### 📂 DİZİN VE VERİTABANI YAPISI

* **Kurulum ve Veri Merkezi:** `C:\VakantieVeilingenBot\`
  - `database.db` : SQLite veritabanı.
  - `Profile\` : Kalıcı WebView2 oturum ve çerez deposu.
  - `Screenshots\` : Otomatik temizlenen anlık görseller havuzu.
* **Program Dizini:** `C:\Users\musta\OneDrive\Desktop\umutistandolandırıcılıkhizmetleri\`
  - `UmutistanDolandiricilikHizmetleri.exe` (Masaüstü Uygulaması)
  - `BotService.exe` (Discord & Web Dashboard Bağımsız Servisi)
  - `Installer.exe` (Kurulum Sihirbazı)
  - `start.bat` (Tek Tıkla Başlatıcı)
