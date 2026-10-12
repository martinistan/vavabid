"""
Umutistan Dolandiricilik Hizmetleri - WhatsApp Assistant Bridge
CallMeBot (Ucretsiz WhatsApp API) ve Ozel Webhook destegi ile anlik WhatsApp bildirim servisi.
"""

import os
import sys
import json
import time
import urllib.parse
import urllib.request
import threading
from db_manager import get_config, set_config, log_event

def send_whatsapp_alert(message, image_path=None):
    """
    Kazanilan veya teklif verilen ihalelerde telefon numarasina WhatsApp uzerinden anlik mesaj gonderir.
    CallMeBot ve Webhook yontemlerini destekler.
    """
    enabled = get_config("whatsapp_enabled", "0")
    if enabled != "1":
        return False, "WhatsApp bildirimleri kapali."

    phone = get_config("whatsapp_target_phone", "").strip()
    apikey = get_config("whatsapp_apikey", "").strip()
    webhook = get_config("whatsapp_webhook_url", "").strip()

    if not phone and not webhook:
        print("[WA] WhatsApp hedef telefon numarasi veya webhook ayarlanmamis.")
        log_event("WHATSAPP", "Gonderim basarisiz: Hedef numara yok.")
        return False, "Telefon numarasi veya Webhook ayarlanmamis."

    # Telefon numarasindaki bosluk ve ozel karakterleri temizle
    clean_phone = phone.replace(" ", "").replace("-", "").replace("(", "").replace(")", "")
    if clean_phone.startswith("+"):
        clean_phone = clean_phone[1:]

    success = False
    details = ""

    # 1. CallMeBot API (Ucretsiz kisisel WhatsApp bildirim servisi)
    if clean_phone and apikey:
        try:
            encoded_msg = urllib.parse.quote(message)
            url = f"https://api.callmebot.com/whatsapp.php?phone={clean_phone}&text={encoded_msg}&apikey={apikey}"
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"})
            with urllib.request.urlopen(req, timeout=10) as resp:
                resp_text = resp.read().decode('utf-8', errors='ignore')
                print(f"[WA] CallMeBot Yaniti: {resp_text}")
                log_event("WHATSAPP", f"{clean_phone} numarasina gonderildi: {message[:50]}...")
                success = True
                details = "CallMeBot ile gonderildi."
        except Exception as e:
            print(f"[WA] CallMeBot Hatasi: {e}")
            log_event("WHATSAPP_HATA", f"CallMeBot hatasi: {e}")
            details = str(e)

    # 2. Ozel Webhook (Opsiyonel alternatif)
    if webhook and not success:
        try:
            payload = json.dumps({"phone": phone, "message": message, "timestamp": time.time()}).encode('utf-8')
            req = urllib.request.Request(webhook, data=payload, headers={"Content-Type": "application/json", "User-Agent": "UmutistanBot/1.0"})
            with urllib.request.urlopen(req, timeout=8) as resp:
                log_event("WHATSAPP", f"Webhook uzerinden gonderildi: {webhook}")
                success = True
                details = "Webhook ile gonderildi."
        except Exception as e:
            print(f"[WA] Webhook Hatasi: {e}")
            log_event("WHATSAPP_HATA", f"Webhook hatasi: {e}")
            details = str(e)

    # Eger hicbiri yoksa sadece yerel log tut
    if not success and clean_phone and not apikey:
        log_event("WHATSAPP", f"Bildirim olusturuldu (API Key girilmedigi icin simule edildi): {phone} -> {message[:60]}")
        return True, "API Key olmadigi icin simule edildi ve loglandi."

    return success, details

def test_whatsapp_alert(phone=None, apikey=None):
    """
    Test mesaji gonderir ve sonucu bildirir.
    """
    p = phone or get_config("whatsapp_target_phone", "")
    k = apikey or get_config("whatsapp_apikey", "")
    
    test_msg = (
        "👑 UMUTİSTAN DOLANDIRICILIK HİZMETLERİ\n"
        "━━━━━━━━━━━━━━━━━━━━━\n"
        "✅ WhatsApp Entegrasyonu Başarılı!\n"
        "🎯 VakantieVeilingen sniper bildirimleriniz artık anında cebinizde."
    )
    
    clean_p = p.replace(" ", "").replace("-", "").replace("(", "").replace(")", "").replace("+", "")
    if clean_p and k:
        try:
            encoded_msg = urllib.parse.quote(test_msg)
            url = f"https://api.callmebot.com/whatsapp.php?phone={clean_p}&text={encoded_msg}&apikey={k}"
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(req, timeout=10) as resp:
                return True, "WhatsApp test mesajı başarıyla iletildi!"
        except Exception as e:
            return False, f"CallMeBot API Hatası: {e}"
    else:
        return False, "Telefon numarası veya CallMeBot API Key eksik."

def run_whatsapp_listener():
    print("[WA] WhatsApp Bildirim Servisi Aktif.")
    while True:
        time.sleep(10)

if __name__ == "__main__":
    run_whatsapp_listener()
