"""
Umutistan Dolandiricilik Hizmetleri - Master Background Service
Discord Botunu, Mobil Web Dashboard'u ve Veritabanini ayni anda yoneten ana servis.
"""

import threading
import time
import os
import sys
import asyncio
import traceback

if sys.platform == 'win32':
    try:
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    except Exception:
        pass

# 1. Tum ciktiyi dosya ve konsola eszamanli yonlendir (Crash & Exit Prevention)
LOG_DIR = r"C:\VakantieVeilingenBot\logs"
os.makedirs(LOG_DIR, exist_ok=True)
LOG_FILE = os.path.join(LOG_DIR, "service.log")

class DualWriter:
    def __init__(self, original, file_path):
        self.original = original
        self.file_path = file_path
        self.encoding = 'utf-8'
        self.errors = 'ignore'
        self._f = None
        try:
            self._f = open(file_path, "a", encoding="utf-8", errors="ignore")
        except:
            pass

    def isatty(self):
        return False

    def write(self, s):
        try:
            if self.original:
                self.original.write(s)
        except:
            pass
        try:
            if self._f:
                self._f.write(s)
                self._f.flush()
        except:
            pass

    def flush(self):
        try:
            if self.original:
                self.original.flush()
        except:
            pass
        try:
            if self._f:
                self._f.flush()
        except:
            pass

sys.stdout = DualWriter(sys.stdout, LOG_FILE)
sys.stderr = DualWriter(sys.stderr, LOG_FILE)

import atexit
def on_process_exit():
    print(f"[EXIT] Python sureci kapaniyor!\n{''.join(traceback.format_stack())}")
atexit.register(on_process_exit)

def handle_global_exc(exc_type, exc_value, exc_traceback):
    print(f"[UNHANDLED EXCEPTION]: {exc_type} {exc_value}\n{''.join(traceback.format_exception(exc_type, exc_value, exc_traceback))}")
sys.excepthook = handle_global_exc

from db_manager import init_db, log_event
import discord_bot
import web_dashboard

def run_web():
    while True:
        try:
            web_dashboard.run_web()
        except Exception as e:
            print(f"[HATA] Web Dashboard beklenmedik sekilde durdu: {e}\n{traceback.format_exc()}")
        time.sleep(3)

def run_discord():
    try:
        discord_bot.run_bot()
    except BaseException as e:
        print(f"[HATA] Discord Bot hatasi ({type(e).__name__}): {e}\n{traceback.format_exc()}")

def main():
    print("=" * 60)
    print("[UMUTISTAN] DOLANDIRICILIK HIZMETLERI - OTONOM MERKEZI SERVIS")
    print("=" * 60)
    
    # 1. Veritabanini baslat
    init_db()
    log_event("SISTEM", "Merkezi servis baslatildi.")

    # 2. Mobil Web Panelini arka plan threadinde calistir (Port 5000)
    t_web = threading.Thread(target=run_web, daemon=True)
    t_web.start()
    print("[OK] Mobil Web Dashboard baslatildi: http://localhost:5000")

    # 3. Discord Botunu calistir ve ana sureci asla sonlandirma
    print("[OK] Discord Bot servisi dongusu baslatiliyor...")
    while True:
        try:
            run_discord()
        except BaseException as ex:
            print(f"[FATAL] run_discord dongu istisnasi ({type(ex).__name__}): {ex}\n{traceback.format_exc()}")
        time.sleep(5)

if __name__ == "__main__":
    main()
