"""
Umutistan Dolandiricilik Hizmetleri - Database Manager
SQLite WAL modunda calisan, eszamanli ve kilitlenmeyen veritabani yonetim modulu.
"""

import sqlite3
import os
import json
from datetime import datetime

DB_PATH = r"C:\VakantieVeilingenBot\database.db"

def get_connection():
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    conn = sqlite3.connect(DB_PATH, timeout=10)
    conn.execute("PRAGMA journal_mode=WAL;")
    conn.execute("PRAGMA busy_timeout=5000;")
    conn.row_factory = sqlite3.Row
    return conn

def init_db():
    conn = get_connection()
    cur = conn.cursor()
    
    # 1. Config / Ayarlar Tablosu
    cur.execute("""
    CREATE TABLE IF NOT EXISTS config (
        key TEXT PRIMARY KEY,
        value TEXT,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    
    # 2. Auctions / Ihaleler Tablosu
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
    
    # 3. Bids / Teklif Gecmisi Tablosu
    cur.execute("""
    CREATE TABLE IF NOT EXISTS bids (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        auction_id INTEGER,
        bid_amount REAL,
        previous_amount REAL,
        status TEXT,
        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY(auction_id) REFERENCES auctions(id)
    );
    """)
    
    # 4. Wins / Kazanim Arsivi Tablosu
    cur.execute("""
    CREATE TABLE IF NOT EXISTS wins (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        auction_id INTEGER,
        title TEXT,
        final_price REAL,
        screenshot_path TEXT,
        discord_sent INTEGER DEFAULT 0,
        whatsapp_sent INTEGER DEFAULT 0,
        won_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY(auction_id) REFERENCES auctions(id)
    );
    """)
    
    # 5. Logs / Sistem ve Olay Kayitlari Tablosu
    cur.execute("""
    CREATE TABLE IF NOT EXISTS logs (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        category TEXT DEFAULT 'SISTEM',
        message TEXT,
        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """)
    
    # Varsayilan ayarlari ekle
    defaults = {
        "discord_token": "",
        "guild_id": "",
        "web_pin": "1234",
        "web_port": "5000",
        "whatsapp_enabled": "0",
        "whatsapp_target_phone": "",
        "whatsapp_apikey": "",
        "whatsapp_webhook_url": "",
        "default_max_bid": "40.0",
        "default_increment": "1.0",
        "default_seconds": "2",
        "stop_group_on_first_win": "1"
    }
    
    for k, v in defaults.items():
        cur.execute("INSERT OR IGNORE INTO config (key, value) VALUES (?, ?)", (k, v))
        
    # Eger settings.json varsa degerleri iceri aktar
    try:
        cfg_file = r"C:\VakantieVeilingenBot\settings.json"
        if os.path.exists(cfg_file):
            with open(cfg_file, "r", encoding="utf-8") as f:
                saved_cfg = json.load(f)
                for k, v in saved_cfg.items():
                    if v is not None and str(v).strip() != "":
                        cur.execute("INSERT INTO config (key, value) VALUES (?, ?) ON CONFLICT(key) DO UPDATE SET value=excluded.value", (k, str(v)))
    except Exception as ex:
        print(f"[DB] settings.json yukleme hatasi: {ex}")

    conn.commit()
    conn.close()
    print("[DB] Veritabani basariyla baslatildi ve tablolar olusturuldu.")

def get_config(key, default=None):
    try:
        conn = get_connection()
        cur = conn.cursor()
        cur.execute("SELECT value FROM config WHERE key = ?", (key,))
        row = cur.fetchone()
        conn.close()
        return row["value"] if row else default
    except:
        return default

def get_all_configs():
    try:
        conn = get_connection()
        cur = conn.cursor()
        cur.execute("SELECT key, value FROM config")
        rows = cur.fetchall()
        conn.close()
        return {r["key"]: r["value"] for r in rows}
    except:
        return {}

def set_config(key, value):
    try:
        conn = get_connection()
        cur = conn.cursor()
        cur.execute("INSERT INTO config (key, value, updated_at) VALUES (?, ?, CURRENT_TIMESTAMP) ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=CURRENT_TIMESTAMP", (key, str(value)))
        if key != "settings_version":
            cur.execute("INSERT INTO config (key, value, updated_at) VALUES ('settings_version', ?, CURRENT_TIMESTAMP) ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=CURRENT_TIMESTAMP", (str(int(datetime.now().timestamp())),))
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"[DB] set_config hatasi: {e}")

def log_event(category, message):
    try:
        conn = get_connection()
        cur = conn.cursor()
        cur.execute("INSERT INTO logs (category, message) VALUES (?, ?)", (category, message))
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"[DB] Loglama hatasi: {e}")

if __name__ == "__main__":
    init_db()
