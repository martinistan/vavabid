[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Umutistan Vavabid - GitHub Push"

$tokenUrl = "https://github.com/settings/tokens/new?description=vavabid-upload&scopes=repo"

$desktop = Join-Path $HOME "OneDrive\Desktop"
if (-not (Test-Path $desktop)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
}

$repoFolder = Get-ChildItem -Path $desktop -Filter "*umutistan*" -Directory | Select-Object -First 1
if (-not $repoFolder) {
    Write-Host "HATA: Proje klasoru Masaustunde bulunamadi!" -ForegroundColor Red
    return
}

Set-Location -LiteralPath $repoFolder.FullName

function Push-With-Token {
    param([string]$tok)
    if (-not $tok -or $tok.Trim() -eq "") {
        Write-Host "Token bos olamaz!" -ForegroundColor Yellow
        return
    }
    $t = $tok.Trim()
    Write-Host ""
    Write-Host "Token ile GitHub'a yukleniyor..." -ForegroundColor Cyan
    git push https://$t@github.com/martinistan/vavabid.git main
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "======================================================================" -ForegroundColor Green
        Write-Host "   TEBRIKLER! REPO BASARIYLA GITHUB'A YUKLENDI!                       " -ForegroundColor Green
        Write-Host "   👉 https://github.com/martinistan/vavabid                          " -ForegroundColor Yellow
        Write-Host "======================================================================" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Yukleme basarisiz oldu. Tokenin dogrulugunu ve 'repo' yetkisini kontrol edin." -ForegroundColor Red
    }
}

while ($true) {
    Clear-Host
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "   UMUTISTAN DOLANDIRICILIK HIZMETLERI (VAVABID) - GITHUB PUSH        " -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "Proje Dizini: $($repoFolder.FullName)" -ForegroundColor Gray
    Write-Host "Hedef Depo  : https://github.com/martinistan/vavabid" -ForegroundColor Gray
    Write-Host "Dal         : main" -ForegroundColor Gray
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Lutfen yapmak istediginiz islemi secin:" -ForegroundColor White
    Write-Host ""
    Write-Host "  [1] Tarayicida Token Olusturma Sayfasini Ac (Tek tikla acar)" -ForegroundColor Yellow
    Write-Host "  [2] Token Yapistir ve Hemen Yukle" -ForegroundColor Green
    Write-Host "  [3] Normal Push Dene (Tarayici oturum acma penceresi dener)" -ForegroundColor Cyan
    Write-Host "  [4] Git Durumunu Goruntule (git status)" -ForegroundColor Gray
    Write-Host "  [0] Cikis" -ForegroundColor DarkGray
    Write-Host ""
    
    $choice = Read-Host "Seciminiz (1/2/3/4/0)"
    
    switch ($choice) {
        "1" {
            Write-Host ""
            Write-Host "Tarayici aciliyor... ($tokenUrl)" -ForegroundColor Green
            Write-Host "Sayfanin en altina inip yesil 'Generate token' butonuna basin ve tokeni kopyalayin." -ForegroundColor Yellow
            Start-Process $tokenUrl
            Write-Host ""
            $tok = Read-Host "Kopyaladiginiz Tokeni buraya yapistirin ve Enter'a basin"
            if ($tok -and $tok.Trim() -ne "") {
                Push-With-Token -tok $tok
            }
            Write-Host ""
            Read-Host "Devam etmek icin Enter'a basin"
        }
        "2" {
            Write-Host ""
            $tok = Read-Host "GitHub Personal Access Tokeninizi yapistirin"
            if ($tok -and $tok.Trim() -ne "") {
                Push-With-Token -tok $tok
            }
            Write-Host ""
            Read-Host "Devam etmek icin Enter'a basin"
        }
        "3" {
            Write-Host ""
            Write-Host "Normal push baslatiliyor... (Tarayici gelirse onaylayin)" -ForegroundColor Cyan
            git push -u origin main
            if ($LASTEXITCODE -eq 0) {
                Write-Host ""
                Write-Host "TEBRIKLER! Basariyla yuklendi!" -ForegroundColor Green
            } else {
                Write-Host ""
                Write-Host "Push tamamlanamadi. Menu'den 1 veya 2'yi secerek Token ile yukleyebilirsiniz." -ForegroundColor Yellow
            }
            Write-Host ""
            Read-Host "Devam etmek icin Enter'a basin"
        }
        "4" {
            Write-Host ""
            git status
            Write-Host ""
            Read-Host "Devam etmek icin Enter'a basin"
        }
        "0" {
            Write-Host "Cikiliyor..." -ForegroundColor Gray
            return
        }
        default {
            Write-Host "Gecersiz secim!" -ForegroundColor Red
            Start-Sleep -Seconds 1
        }
    }
}
