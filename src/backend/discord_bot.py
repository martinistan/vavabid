# ============================================================================
# MARTINISTAN VAVABID DISCORD SUITE & AUTONOMOUS GATEWAY
# Architected & Developed by: Martinistan / Martiny (@martinistan)
# ============================================================================

MARTINY_AUTHOR = "martinistan"
MARTINY_VERSION = "2.0"

import discord
from discord import app_commands
from discord.ext import commands
import asyncio
import aiohttp
from aiohttp import web
import os
import sys
import json
import sqlite3
import datetime
import psutil

if sys.platform == 'win32':
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='ignore')
        sys.stderr.reconfigure(encoding='utf-8', errors='ignore')
    except Exception:
        pass
    try:
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    except Exception:
        pass

from db_manager import get_connection, get_config, set_config, log_event
import whatsapp_assistant

INTENTS = discord.Intents.default()
INTENTS.guilds = True
INTENTS.guild_messages = True

bot = commands.Bot(command_prefix="!", intents=INTENTS)
tree = bot.tree

IPC_DESKTOP_URL = "http://127.0.0.1:58432"
EVENT_PORT = 58433

class AuctionActionView(discord.ui.View):
    def __init__(self, auction_url, title):
        super().__init__(timeout=None)
        self.auction_url = auction_url
        self.title = title

    @discord.ui.button(label="➕ +5€ Ekle", style=discord.ButtonStyle.green, custom_id="btn_add_5")
    async def add_five(self, interaction: discord.Interaction, button: discord.ui.Button):
        await interaction.response.defer(ephemeral=True)
        try:
            async with aiohttp.ClientSession() as session:
                # IPC uzerinden limiti 5 artir
                async with session.get(f"{IPC_DESKTOP_URL}/status") as resp:
                    if resp.status == 200:
                        data = await resp.json()
                        current_item = next((x for x in data if x.get("url") == self.auction_url), None)
                        if current_item:
                            new_max = current_item.get("max", 40) + 5
                            async with session.get(f"{IPC_DESKTOP_URL}/add?url={self.auction_url}&max={new_max}"):
                                pass
                            await interaction.followup.send(f"✅ **{self.title}** için üst limit 5 € artırıldı: **{new_max} €**", ephemeral=True)
                            return
            await interaction.followup.send("⚠️ İhale bulunamadı veya masaüstü uygulaması kapalı.", ephemeral=True)
        except Exception as e:
            await interaction.followup.send(f"Hata: {e}", ephemeral=True)

    @discord.ui.button(label="⏹️ Durdur", style=discord.ButtonStyle.danger, custom_id="btn_stop_auc")
    async def stop_auction(self, interaction: discord.Interaction, button: discord.ui.Button):
        await interaction.response.defer(ephemeral=True)
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{IPC_DESKTOP_URL}/stop?url={self.auction_url}"):
                    pass
            await interaction.followup.send(f"⏹️ **{self.title}** takibi durduruldu.", ephemeral=True)
        except Exception as e:
            await interaction.followup.send(f"Hata: {e}", ephemeral=True)

    @discord.ui.button(label="📸 Anlık SS", style=discord.ButtonStyle.primary, custom_id="btn_ss_auc")
    async def snap_screen(self, interaction: discord.Interaction, button: discord.ui.Button):
        await interaction.response.defer(ephemeral=True)
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{IPC_DESKTOP_URL}/screenshot") as resp:
                    if resp.status == 200:
                        data = await resp.json()
                        img_path = data.get("path")
                        if img_path and os.path.exists(img_path):
                            file = discord.File(img_path, filename="screenshot.png")
                            embed = discord.Embed(
                                title=f"📸 Anlık Görünüm: {self.title}",
                                description="Canlı ihale sayfasının anlık 4K ekran görüntüsü.",
                                color=0x10b981
                            )
                            embed.set_image(url="attachment://screenshot.png")
                            await interaction.followup.send(embed=embed, file=file)
                            
                            # YUKLENDIKTEN SONRA YEREL DOSYAYI ANINDA SIL (SIFIR DISK KIRLILIGI)
                            try:
                                os.remove(img_path)
                            except:
                                pass
                            return
            await interaction.followup.send("⚠️ Ekran görüntüsü alınamadı.", ephemeral=True)
        except Exception as e:
            await interaction.followup.send(f"Hata: {e}", ephemeral=True)

async def periodic_status_updater():
    await bot.wait_until_ready()
    while not bot.is_closed():
        await asyncio.sleep(45)
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{IPC_DESKTOP_URL}/status", timeout=2) as resp:
                    if resp.status == 200:
                        items = await resp.json()
                        if items and len(items) > 0:
                            desc = ""
                            for it in items[:6]:
                                armed_icon = "🟢" if it.get("armed") else "⏸️"
                                desc += f"{armed_icon} **{it.get('title', 'İhale')}**\n"
                                desc += f"   💵 Sitedeki: `{it.get('currentBid', 0):.2f} €` | ⏱️ `{it.get('timerText', '—')}` | 🎯 Limit: `{it.get('max', 32):.2f} €`\n\n"
                            
                            embed = discord.Embed(
                                title="📡 Canlı İhale Takip Durumu (Otomatik Senkron)",
                                description=desc,
                                color=0x10b981,
                                timestamp=datetime.datetime.now()
                            )
                            embed.set_footer(text="Masaüstü Sniper ile anlık senkronize")
                            await send_to_channel("ch_aktif_ihaleler", embed=embed)
        except:
            pass

async def bot_setup_hook():
    print("[IPC] setup_hook: IPC dinleyicisi baslatiliyor...")
    bot.loop.create_task(start_event_receiver())
    bot.loop.create_task(periodic_status_updater())

bot.setup_hook = bot_setup_hook

@bot.event
async def on_ready():
    try:
        print(f"[OK] Discord Botu Basariyla Baglandi: {bot.user} (ID: {bot.user.id})")

        # 2. Sunucularda gizli kategorileri ve kanallari kur
        for guild in bot.guilds:
            try:
                await ensure_private_channels(guild)
            except Exception as eg:
                print(f"Kanal olusturma hatasi ({guild.name}): {eg}")

        # 3. Slash komutlarini global sync et
        try:
            synced = await tree.sync()
            print(f"[OK] {len(synced)} adet Slash Komutu basariyla senkronize edildi.")
        except Exception as e:
            print(f"Slash komut senkronizasyon hatasi: {e}")

        # 4. Sistem kanalina baslangic bildirimi gonder
        try:
            await send_to_channel("ch_log_sistem", embed=discord.Embed(
                title="🟢 Umutistan Merkezi Servis Çevrimiçi",
                description=f"**Bot:** `{bot.user}`\n"
                            f"**Sunucular:** {len(bot.guilds)}\n"
                            f"**Zaman:** `{datetime.datetime.now().strftime('%d.%m.%Y %H:%M:%S')}`\n"
                            f"Masaüstü IPC dinleyicisi ve Mobil Web Dashboard aktif.",
                color=0x10b981
            ))
        except:
            pass
    except Exception as ex:
        print(f"on_ready genel hatasi: {ex}")

def normalize_url(url):
    url = url.strip()
    if url.startswith("/"):
        return f"https://www.vakantieveilingen.be{url}"
    if not url.startswith("http://") and not url.startswith("https://"):
        return f"https://www.vakantieveilingen.be/{url}"
    return url

async def ensure_private_channels(guild, user=None):
    """
    Kategori ve kanallari olusturur.
    @everyone rollerinin tum okuma/gorme izinlerini IPTAL eder.
    Komutu calistiran kullaniciya ve sunucu sahibine erisim verir.
    """
    category_name = "🔒 VAKANTIE-BOT YÖNETİM"
    category = discord.utils.get(guild.categories, name=category_name)

    everyone_role = guild.default_role
    bot_member = guild.me

    overwrites = {
        everyone_role: discord.PermissionOverwrite(
            view_channel=False,
            read_messages=False,
            read_message_history=False,
            send_messages=False
        ),
        bot_member: discord.PermissionOverwrite(
            view_channel=True,
            read_messages=True,
            read_message_history=True,
            send_messages=True,
            embed_links=True,
            attach_files=True,
            manage_channels=True
        )
    }

    if user:
        overwrites[user] = discord.PermissionOverwrite(
            view_channel=True,
            read_messages=True,
            read_message_history=True,
            send_messages=True
        )

    try:
        if guild.owner:
            overwrites[guild.owner] = discord.PermissionOverwrite(
                view_channel=True,
                read_messages=True,
                read_message_history=True,
                send_messages=True
            )
        elif guild.owner_id:
            owner = await guild.fetch_member(guild.owner_id)
            if owner:
                overwrites[owner] = discord.PermissionOverwrite(
                    view_channel=True,
                    read_messages=True,
                    read_message_history=True,
                    send_messages=True
                )
    except Exception as ex_owner:
        print(f"Sunucu sahibi alinamadi: {ex_owner}")

    if not category:
        try:
            category = await guild.create_category(category_name, overwrites=overwrites)
            print(f"[OK] Gizli Kategori olusturuldu: {category_name} ({guild.name})")
        except Exception as e:
            print(f"Kategori olusturulamadi: {e}")
            return False, f"Kategori oluşturulamadı: {e}"
    else:
        if user:
            try:
                await category.set_permissions(user, view_channel=True, read_messages=True, send_messages=True)
            except Exception as e:
                print(f"Kategori izin guncelleme hatasi: {e}")

    channel_defs = [
        ("log-teklifler", "📝 Verilen tüm tekliflerin detaylı anlık dökümü.", "ch_log_teklifler"),
        ("log-hatalar", "⚠️ Bağlantı, bütçe aşımı veya site hata bildirimleri.", "ch_log_hatalar"),
        ("log-sistem", "⚙️ Bot ve masaüstü açılış/kapanış olayları.", "ch_log_sistem"),
        ("bildirimler", "🔔 Son saniye ve kritik ihale bildirimleri.", "ch_bildirimler"),
        ("aktif-ihaleler", "📌 Canlı izlenen açık artırmalar ve interaktif butonlar.", "ch_aktif_ihaleler"),
        ("kazanılanlar-arşiv", "🏆 Kazanılan tüm açık artırmalar ve kanıt fotoğrafları.", "ch_kazanilanlar_arsiv")
    ]

    for name, topic, cfg_key in channel_defs:
        existing = discord.utils.get(category.text_channels, name=name)
        if not existing:
            try:
                ch = await guild.create_text_channel(name, category=category, topic=topic, overwrites=overwrites)
                print(f"  + Kanal olusturuldu: #{name}")
                set_config(cfg_key, str(ch.id))
                set_config(f"ch_{name.replace('-', '_')}", str(ch.id))
            except Exception as e:
                print(f"Kanal olusturulamadi ({name}): {e}")
        else:
            set_config(cfg_key, str(existing.id))
            set_config(f"ch_{name.replace('-', '_')}", str(existing.id))
            if user:
                try:
                    await existing.set_permissions(user, view_channel=True, read_messages=True, send_messages=True)
                except:
                    pass

        # Bos kanal birakma: Her kanala ozel baslangic durumu gonder
        try:
            target_ch = existing or ch
            has_msgs = False
            async for _ in target_ch.history(limit=1):
                has_msgs = True
                break
            if not has_msgs:
                init_embeds = {
                    "log-teklifler": discord.Embed(
                        title="📝 Otomatik Teklif Log Kanalı Devrede",
                        description="Sniper tarafından sunucuya iletilen her teklif, tutar ve ihale bilgisiyle burada anlık olarak kayıt altına alınır.",
                        color=0x10b981
                    ),
                    "log-hatalar": discord.Embed(
                        title="⚠️ Hata & Uyarı Teşhis Kanalı Devrede",
                        description="Ağ kopmaları, giriş yapılmama veya bütçe limit aşım uyarıları bu kanala raporlanır. (Şu an sistem hatasız ve çevrimiçi).",
                        color=0xef4444
                    ),
                    "log-sistem": discord.Embed(
                        title="⚙️ Sistem & Altyapı Olayları Kanalı Devrede",
                        description=f"Bot başlatıldı, Masaüstü IPC bağlantısı aktif ve hazır.\nZaman: `{datetime.datetime.now().strftime('%d.%m.%Y %H:%M:%S')}`",
                        color=0x3b82f6
                    ),
                    "bildirimler": discord.Embed(
                        title="🔔 Kritik Bildirimler & Son Saniye Odası",
                        description="Kritik geri sayım (1-15 sn kala) ve acil teklif alarmları bu kanala düşer.",
                        color=0xf59e0b
                    ),
                    "aktif-ihaleler": discord.Embed(
                        title="📌 Aktif İhale Takip Paneli",
                        description="Şu an masaüstünde izlenen açık artırmalar burada listelenir.\nYeni ihale eklemek için: `/ekle [url] [max:32] [artis:1] [sonsn:1]` komutunu kullanabilirsiniz.",
                        color=0x8b5cf6
                    ),
                    "kazanılanlar-arşiv": discord.Embed(
                        title="🏆 Kazanılan İhaleler Arşivi",
                        description="Kazanılan tüm ihaleler, nihai tutar ve 4K kanıt ekran görüntüleriyle burada kalıcı olarak arşivlenir.",
                        color=0xf59e0b
                    )
                }
                emb = init_embeds.get(name)
                if emb:
                    await target_ch.send(embed=emb)
        except Exception as ex_init:
            print(f"Kanal baslangic mesaji gonderilemedi ({name}): {ex_init}")

    return True, "Başarılı"

# --- SLASH KOMUTLARI ---

@tree.command(name="kurulum", description="Gizli kategori ve 6 özel kanalı sunucuda otomatik yapılandırır.")
async def cmd_setup(interaction: discord.Interaction):
    await interaction.response.defer(ephemeral=True)
    if not interaction.guild:
        await interaction.followup.send("Bu komut sadece bir sunucuda çalıştırılabilir.", ephemeral=True)
        return
    ok, err = await ensure_private_channels(interaction.guild, interaction.user)
    if ok:
        await interaction.followup.send("✅ Gizli kategori (`🔒 VAKANTIE-BOT YÖNETİM`) ve 6 kanal başarıyla kuruldu!", ephemeral=True)
    else:
        await interaction.followup.send(f"⚠️ Kurulum hatası: {err}", ephemeral=True)

@tree.command(name="ekle", description="Yeni bir VakantieVeilingen açık artırmasını uzaktan takibe alır.")
@app_commands.describe(
    url="Açık artırma linki veya /veilingen/... /dagje-uit/... yolu",
    max="Maksimum tavan teklif tutarınız (€) (Varsayılan: 32 €)",
    artis="Mevcut fiyatın üstüne eklenecek artış tutarı (Varsayılan: +1 €)",
    sonsn="Teklifin basılacağı son saniye eşiği (Varsayılan: 1 sn - Son Saniye)"
)
async def cmd_add(interaction: discord.Interaction, url: str, max: float = 32.0, artis: float = 1.0, sonsn: int = 1):
    await interaction.response.defer()
    
    # URL normallestirme
    url = normalize_url(url)
    if "vakantieveilingen" not in url.lower():
        await interaction.followup.send("❌ Geçerli bir VakantieVeilingen linki giriniz (.nl veya .be).")
        return

    # Veritabanina ekle
    try:
        conn = get_connection()
        cur = conn.cursor()
        cur.execute("""
            INSERT INTO auctions (url, max_bid, increment, trigger_seconds, status, is_active)
            VALUES (?, ?, ?, ?, 'IZLENIYOR', 1)
            ON CONFLICT(url) DO UPDATE SET max_bid=excluded.max_bid, increment=excluded.increment, trigger_seconds=excluded.trigger_seconds
        """, (url, max, artis, sonsn))
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"DB Ekleme Hatasi: {e}")

    # Masaustu uygulamaya IPC uzerinden sekme actir
    ipc_success = False
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/add?url={url}&max={max}&step={artis}&sec={sonsn}", timeout=3) as resp:
                if resp.status == 200:
                    ipc_success = True
    except Exception as e:
        print(f"IPC Hatasi: {e}")

    # Discord mesaj karti
    embed = discord.Embed(
        title="🎯 Yeni Açık Artırma Takibe Alındı",
        description=f"**Bağlantı:** [İhaleye Git]({url})\n\n"
                    f"💶 **Üst Limit:** `{max:.2f} €`\n"
                    f"📈 **Artış Miktarı:** `+{artis:.2f} €`\n"
                    f"⏱️ **Tetikleme Saniyesi:** `{sonsn} sn (Son Saniye)`\n"
                    f"🖥️ **Masaüstü Bağlantısı:** {'🟢 Sekme Açıldı' if ipc_success else '⚠️ Beklemede'}",
        color=0x10b981,
        timestamp=datetime.datetime.now()
    )
    view = AuctionActionView(url, "İhale")
    await interaction.followup.send(embed=embed, view=view)

    # Aktif ihaleler kanalina da duyuru at
    try:
        await send_to_channel("ch_aktif_ihaleler", embed=embed)
    except:
        pass

@tree.command(name="izle", description="Yeni bir VakantieVeilingen açık artırmasını uzaktan takibe alır (/ekle ile aynıdır).")
@app_commands.describe(
    url="Açık artırma linki veya /veilingen/... /dagje-uit/... yolu",
    max="Maksimum tavan teklif tutarınız (€) (Varsayılan: 32 €)",
    artis="Mevcut fiyatın üstüne eklenecek artış tutarı (Varsayılan: +1 €)",
    sonsn="Teklifin basılacağı son saniye eşiği (Varsayılan: 1 sn - Son Saniye)"
)
async def cmd_watch(interaction: discord.Interaction, url: str, max: float = 32.0, artis: float = 1.0, sonsn: int = 1):
    await cmd_add(interaction, url, max, artis, sonsn)

@tree.command(name="liste", description="Şu an izlenen aktif açık artırmaları ve anlık fiyatlarını listeler.")
async def cmd_list(interaction: discord.Interaction):
    await interaction.response.defer()
    
    active_items = []
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/status", timeout=2) as resp:
                if resp.status == 200:
                    active_items = await resp.json()
    except:
        pass

    embed = discord.Embed(
        title="📋 Aktif Takipteki Açık Artırmalar",
        color=0x3b82f6,
        timestamp=datetime.datetime.now()
    )

    if not active_items:
        embed.description = "Şu anda masaüstünde açık olan aktif bir ihale sekmesi bulunmuyor."
    else:
        for item in active_items[:10]:
            title = item.get("title") or "İhale"
            cur_bid = item.get("currentBid", 0)
            timer = item.get("timerText", "—")
            armed = "🟢 İzleniyor" if item.get("armed") else "⏸️ Beklemede"
            max_val = item.get("max", 0)
            
            embed.add_field(
                name=f"◈ {title[:35]}",
                value=f"Fiyat: **{cur_bid} €** | Sayaç: **{timer}** | Limit: **{max_val} €** | Durum: {armed}",
                inline=False
            )

    await interaction.followup.send(embed=embed)

@tree.command(name="durdur", description="Tüm ihaleleri veya masaüstü otomatik izlemeyi durdurur.")
async def cmd_stop_all(interaction: discord.Interaction):
    await interaction.response.defer()
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/stop?all=true", timeout=2):
                pass
        await interaction.followup.send("⏹️ Tüm aktif otomatik teklif takipleri durduruldu.")
    except Exception as e:
        await interaction.followup.send(f"Masaüstü uygulamasına bağlanılamadı: {e}")

@tree.command(name="limit", description="Masaüstündeki aktif açık artırmanın bütçe limitini günceller.")
@app_commands.describe(yeni_limit="Yeni maksimum teklif limiti (örn: 35, 45, 50)")
async def cmd_set_limit(interaction: discord.Interaction, yeni_limit: float):
    await interaction.response.defer()
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/set_limit?max={yeni_limit}", timeout=3) as resp:
                if resp.status == 200:
                    await interaction.followup.send(f"✅ İhale maksimum bütçe limiti başarıyla `{yeni_limit:.2f} €` olarak güncellendi!")
                    return
        await interaction.followup.send("❌ Masaüstü uygulamasına bağlanılamadı veya işlem başarısız oldu.")
    except Exception as e:
        await interaction.followup.send(f"❌ Hata: {e}")

@tree.command(name="kapat_iptal", description="Geri sayımı başlayan bilgisayar kapatma işlemini iptal eder.")
async def cmd_abort_shutdown(interaction: discord.Interaction):
    await interaction.response.defer()
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/shutdown?cmd=abort", timeout=3) as resp:
                if resp.status == 200:
                    await interaction.followup.send("✅ Bilgisayar kapatma işlemi iptal edildi!")
                    return
        await interaction.followup.send("❌ Masaüstü uygulamasına bağlanılamadı.")
    except Exception as e:
        await interaction.followup.send(f"❌ Hata: {e}")

@tree.command(name="ss", description="Masaüstündeki aktif açık artırmanın anlık 4K ekran görüntüsünü alır.")
async def cmd_screenshot(interaction: discord.Interaction):
    await interaction.response.defer()
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/screenshot", timeout=5) as resp:
                if resp.status == 200:
                    data = await resp.json()
                    img_path = data.get("path")
                    if img_path and os.path.exists(img_path):
                        file = discord.File(img_path, filename="screenshot.png")
                        embed = discord.Embed(
                            title="📸 Canlı Masaüstü Ekran Görüntüsü",
                            description="VakantieVeilingen açık artırma ekranının anlık durumu.",
                            color=0x10b981,
                            timestamp=datetime.datetime.now()
                        )
                        embed.set_image(url="attachment://screenshot.png")
                        await interaction.followup.send(embed=embed, file=file)
                        
                        # Discord CDN'e yuklendikten hemen sonra yerel PNG dosyasini sil
                        try:
                            os.remove(img_path)
                        except:
                            pass
                        return
        await interaction.followup.send("⚠️ Ekran görüntüsü alınamadı (Masaüstü uygulaması açık mı?).")
    except Exception as e:
        await interaction.followup.send(f"Hata oluştu: {e}")

@tree.command(name="durum", description="Botun, bilgisayarın ve motorun genel sağlık durumunu gösterir.")
async def cmd_status(interaction: discord.Interaction):
    await interaction.response.defer()
    
    cpu = psutil.cpu_percent()
    ram = psutil.virtual_memory().percent
    
    # IPC kontrol
    ipc_online = False
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(f"{IPC_DESKTOP_URL}/status", timeout=1) as resp:
                ipc_online = (resp.status == 200)
    except:
        pass

    embed = discord.Embed(title="⚡ Sistem Sağlık Raporu", color=0x10b981)
    embed.add_field(name="💻 CPU Kullanımı", value=f"`%{cpu}`", inline=True)
    embed.add_field(name="🧠 Bellek (RAM)", value=f"`%{ram}`", inline=True)
    embed.add_field(name="🔗 Masaüstü IPC Köprüsü", value="🟢 Aktif" if ipc_online else "🔴 Kapalı", inline=True)
    embed.add_field(name="🤖 Bot Gecikmesi", value=f"`{round(bot.latency * 1000)} ms`", inline=True)
    
    await interaction.followup.send(embed=embed)

@tree.command(name="yardim", description="Mevcut tüm slash komutlarını ve ipuçlarını gösterir.")
async def cmd_help(interaction: discord.Interaction):
    embed = discord.Embed(
        title="📖 VakantieVeilingen Bot Yardım Rehberi",
        description="Bilgisayar başında olmadan tüm açık artırmaları Discord üzerinden yönetebilirsiniz:",
        color=0x10b981
    )
    embed.add_field(name="/ekle [url] [max] [artis] [sonsn]", value="Yeni bir ihaleyi uzaktan takibe alır ve butonlu kartını oluşturur.", inline=False)
    embed.add_field(name="/liste", value="Canlı izlenen tüm ihaleleri ve güncel fiyatlarını listeler.", inline=False)
    embed.add_field(name="/ss", value="Anlık 4K ekran görüntüsü çeker ve gönderir (yerel resim otomatik silinir).", inline=False)
    embed.add_field(name="/durdur", value="Tüm otomatik izlemeleri durdurur.", inline=False)
    embed.add_field(name="/kurulum", value="Gizli kategori ve 6 özel kanalı yeniden kurar.", inline=False)
    embed.add_field(name="/durum", value="Sistem ve bellek durumunu gösterir.", inline=False)
    await interaction.response.send_message(embed=embed)

# --- IPC EVENT RECEIVER (C# -> DISCORD & WHATSAPP) ---

async def send_to_channel(ch_key, content=None, embed=None, file=None):
    ch_id = get_config(ch_key)
    if not ch_id and ch_key == "ch_kazanilanlar_arsiv":
        ch_id = get_config("ch_kazanılanlar_arşiv")
    if not ch_id:
        print(f"[DISCORD] Kanal ID tanimli degil: {ch_key}")
        return False
    try:
        cid = int(ch_id)
        channel = bot.get_channel(cid)
        if not channel:
            channel = await bot.fetch_channel(cid)
        if channel:
            await channel.send(content=content, embed=embed, file=file)
            return True
    except Exception as ex:
        print(f"[DISCORD] Kanala gonderim basarisiz ({ch_key}): {ex}")
    return False

async def handle_desktop_event(request):
    try:
        try:
            data = await request.json()
        except:
            raw = await request.read()
            try:
                text = raw.decode("utf-8", errors="replace")
            except:
                text = raw.decode("latin-1", errors="replace")
            data = json.loads(text)
        event_type = str(data.get("type", "")).upper()
        title = data.get("title", "İhale")
        price = float(data.get("price", 0) or 0)
        image_path = data.get("image")
        details = data.get("details", "")

        print(f"[IPC] Masaustu Olayi: {event_type} | {title} | {price} EUR | {details}")
        now_str = datetime.datetime.now().strftime("%d.%m.%Y %H:%M:%S")

        # 1. KAZANILAN İHALE (WIN) -> #kazanılanlar-arşiv & WhatsApp
        if event_type == "WIN":
            try:
                wa_msg = (
                    f"🏆 TEBRİKLER! İHALE KAZANILDI!\n"
                    f"━━━━━━━━━━━━━━━━━━━━━\n"
                    f"🎯 İhale: {title}\n"
                    f"💶 Fiyat: {price:.2f} € (Tahmini Toplam: {price + 5.50:.2f} €)\n"
                    f"⏱️ Tarih: {now_str}"
                )
                whatsapp_assistant.send_whatsapp_alert(wa_msg, image_path)
            except Exception as ex_wa:
                print(f"[WA Alert Hatasi]: {ex_wa}")

            embed = discord.Embed(
                title=f"🏆 TEBRİKLER! İHALE KAZANILDI!",
                description=f"**{title}** açık artırması başarıyla kazanıldı!\n\n"
                            f"💶 **Kazanılan Fiyat:** `{price:.2f} €`\n"
                            f"📌 **Tahmini Toplam (+Masraflar):** `{price + 5.50:.2f} €`\n"
                            f"⏱️ **Tarih:** `{now_str}`",
                color=0xf59e0b,
                timestamp=datetime.datetime.now()
            )
            file = None
            if image_path and os.path.exists(image_path):
                file = discord.File(image_path, filename="win.png")
                embed.set_image(url="attachment://win.png")

            await send_to_channel("ch_kazanilanlar_arsiv", content="@everyone 🏆 **KAZANDIK!**", embed=embed, file=file)
            if image_path and os.path.exists(image_path):
                try: os.remove(image_path)
                except: pass

        # 2. VERİLEN TEKLİF (BID) -> #log-teklifler
        elif event_type == "BID":
            embed = discord.Embed(
                title="🎯 Teklif Verildi",
                description=f"**İhale:** {title}\n"
                            f"💶 **Teklif:** `{price:.2f} €`\n"
                            f"ℹ️ **Detay:** {details or 'Otomatik teklif başarıyla iletildi.'}",
                color=0x10b981,
                timestamp=datetime.datetime.now()
            )
            await send_to_channel("ch_log_teklifler", embed=embed)

        # 3. SİSTEM VE BAŞLANGIÇ/KAPANIŞ (SYSTEM) -> #log-sistem
        elif event_type == "SYSTEM":
            embed = discord.Embed(
                title="⚙️ Sistem Olayı",
                description=f"**Konu:** {title}\n"
                            f"ℹ️ **Detay:** {details or 'Masaüstü uygulaması durumu güncellendi.'}\n"
                            f"⏱️ **Zaman:** `{now_str}`",
                color=0x3b82f6,
                timestamp=datetime.datetime.now()
            )
            await send_to_channel("ch_log_sistem", embed=embed)

        # 4. HATA VE BÜTÇE UYARILARI (ERROR) -> #log-hatalar
        elif event_type == "ERROR":
            embed = discord.Embed(
                title="⚠️ Hata / Uyarı",
                description=f"**Başlık:** {title}\n"
                            f"🚨 **Açıklama:** {details or 'Bilinmeyen hata oluştu.'}\n"
                            f"⏱️ **Zaman:** `{now_str}`",
                color=0xef4444,
                timestamp=datetime.datetime.now()
            )
            await send_to_channel("ch_log_hatalar", embed=embed)

        # 5. KRİTİK GERİ SAYIM & SNIPER BİLDİRİMİ (ALERT) -> #bildirimler
        elif event_type in ("ALERT", "NOTIFICATION"):
            try:
                wa_msg = (
                    f"⚠️ UMUTİSTAN LİMİT / BİLDİRİM UYARISI!\n"
                    f"━━━━━━━━━━━━━━━━━━━━━\n"
                    f"🎯 İhale: {title}\n"
                    f"💶 Tutar: {price:.2f} €\n"
                    f"ℹ️ Durum: {details or 'Kritik uyarı'}\n"
                    f"⏱️ Tarih: {now_str}"
                )
                whatsapp_assistant.send_whatsapp_alert(wa_msg)
            except Exception as ex_wa:
                print(f"[WA Alert Hatasi]: {ex_wa}")

            embed = discord.Embed(
                title="🔔 Kritik İhale Uyarısı",
                description=f"**İhale:** {title}\n"
                            f"💶 **Güncel Tutar:** `{price:.2f} €`\n"
                            f"⏱️ **Kalan Süre / Durum:** {details}\n"
                            f"📅 **Zaman:** `{now_str}`",
                color=0xf59e0b,
                timestamp=datetime.datetime.now()
            )
            await send_to_channel("ch_bildirimler", embed=embed)

        # 6. AKTİF İHALE TAKİBİ (AUCTION) -> #aktif-ihaleler
        elif event_type == "AUCTION":
            embed = discord.Embed(
                title="📌 Yeni İhale Takipte",
                description=f"**İhale:** {title}\n"
                            f"💶 **Maksimum Bütçe:** `{price:.2f} €`\n"
                            f"🔗 **Detay:** {details}",
                color=0x8b5cf6,
                timestamp=datetime.datetime.now()
            )
            await send_to_channel("ch_aktif_ihaleler", embed=embed)

        return web.json_response({"ok": True})
    except Exception as e:
        print(f"[IPC] Hata: {e}")
        return web.json_response({"error": str(e)}, status=500)

async def handle_test_whatsapp(request):
    try:
        data = await request.json()
        phone = data.get("phone")
        apikey = data.get("apikey")
    except:
        phone = request.query.get("phone")
        apikey = request.query.get("apikey")
    ok, msg = whatsapp_assistant.test_whatsapp_alert(phone, apikey)
    return web.json_response({"ok": ok, "message": msg})

async def start_event_receiver():
    app = web.Application()
    app.router.add_post("/event", handle_desktop_event)
    app.router.add_post("/test_whatsapp", handle_test_whatsapp)
    app.router.add_get("/test_whatsapp", handle_test_whatsapp)
    runner = web.AppRunner(app)
    await runner.setup()
    site = web.TCPSite(runner, "127.0.0.1", EVENT_PORT)
    await site.start()
    print(f"[OK] Masaustu IPC Event Dinleyicisi Aktif: http://127.0.0.1:{EVENT_PORT}/event")

def run_bot():
    token = get_config("discord_token") or os.environ.get("DISCORD_TOKEN")
    if not token or len(token.strip()) < 10:
        # settings.json dosyasini kontrol et
        try:
            cfg_file = r"C:\VakantieVeilingenBot\settings.json"
            if os.path.exists(cfg_file):
                with open(cfg_file, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    token = data.get("discord_token")
        except:
            pass

    if not token or len(token.strip()) < 10:
        print("[UYARI] Discord Bot Token bulunamadi! Bot beklemede calisacak. Ayarlardan veya C:\\VakantieVeilingenBot\\settings.json uzerinden token girildiginde otomatik baglanir.")
        # Event receiver'i yine de calistir ki yerel masaustu kopmasin
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        loop.run_until_complete(start_event_receiver())
        loop.run_forever()
        return

    import logging
    log_file = r"C:\VakantieVeilingenBot\logs\service.log"
    os.makedirs(os.path.dirname(log_file), exist_ok=True)
    f_handler = logging.FileHandler(log_file, encoding='utf-8')
    f_handler.setFormatter(logging.Formatter('[%(asctime)s] [%(levelname)s] %(name)s: %(message)s'))
    bot.run(token, log_handler=f_handler)

if __name__ == "__main__":
    run_bot()
