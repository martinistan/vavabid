@echo off
chcp 65001 >nul
title Umutistan Dolandırıcılık Hizmetleri - Başlatıcı
cd /d "%~dp0"

echo ======================================================================
echo ◈  UMUTİSTAN DOLANDIRICILIK HİZMETLERİ - OTONOM AÇIK ARTIRMA SİSTEMİ
echo ======================================================================
echo.

:: 1. C: Dizini Kontrolü
if not exist "C:\VakantieVeilingenBot\database.db" (
    echo [!] İlk çalıştırma algılandı. Kurulum sihirbazı başlatılıyor...
    if exist "Installer.exe" (
        start /wait "" "Installer.exe"
    ) else (
        python Installer.py
    )
)

:: 2. Arka Plan Otonom Servisi (Discord Bot + Mobil Web Dashboard Port 5000)
echo [*] Arka plan Discord ve Mobil Web Dashboard servisi başlatılıyor...
if exist "BotService.exe" (
    start "Umutistan Bot Servisi" /min "BotService.exe"
) else (
    start "Umutistan Bot Servisi" /min python bot_service.py
)

:: 3. Kısa bekleme (IPC servisinin ayağa kalkması için)
timeout /t 2 /nobreak >nul

:: 4. Ana Masaüstü Arayüzü (WebView2 Saf Siyah)
echo [*] Masaüstü arayüzü açılıyor...
start "" "UmutistanDolandiricilikHizmetleri.exe"

echo [✓] Tüm sistem başarıyla başlatıldı!
exit
