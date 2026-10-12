[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Umutistan Vavabid - GitHub Push"

Clear-Host
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "   UMUTISTAN DOLANDIRICILIK HIZMETLERI (VAVABID) - GITHUB PUSH        " -ForegroundColor Yellow
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

$desktop = Join-Path $HOME "OneDrive\Desktop"
if (-not (Test-Path $desktop)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
}

$repoFolder = Get-ChildItem -Path $desktop -Filter "*umutistan*" -Directory | Select-Object -First 1
if (-not $repoFolder) {
    Write-Host "HATA: Proje klasoru Masaustunde bulunamadi!" -ForegroundColor Red
    Write-Host "Cikmak icin bir tusa basin..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Set-Location -LiteralPath $repoFolder.FullName
Write-Host "Proje Dizini: $($repoFolder.FullName)" -ForegroundColor Gray
Write-Host "Hedef Depo  : https://github.com/martinistan/vavabid" -ForegroundColor Gray
Write-Host "Dal         : main" -ForegroundColor Gray
Write-Host ""

$tokenUrl = "https://github.com/settings/tokens/new?description=vavabid-upload&scopes=repo"

Write-Host "GitHub'a yukleniyor..." -ForegroundColor Green
Write-Host "(Tarayici acilirsa 'Sign in with your browser' ile giris yapin)" -ForegroundColor Gray
Write-Host ""

git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Green
    Write-Host "   TEBRIKLER! REPO BASARIYLA GITHUB'A YUKLENDI!                       " -ForegroundColor Green
    Write-Host "   Link: https://github.com/martinistan/vavabid                       " -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Red
    Write-Host "   GIRIS GEREKIYOR VEYA HATA OLUSTU                                   " -ForegroundColor Red
    Write-Host "======================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Hic aramakla ugrasmamaniz icin dogrudan Token alma linki hazirlandi:" -ForegroundColor Yellow
    Write-Host "Link: $tokenUrl" -ForegroundColor Cyan
    Write-Host ""
    
    $openBrowser = Read-Host "Token sayfasini tarayicinizda simdi acalim mi? (E/H - Varsayilan: Evet)"
    if ($openBrowser -eq "" -or $openBrowser -match "^[eEyY]") {
        Write-Host "Tarayici aciliyor... (Sayfanin en altindaki 'Generate token' butonuna tiklayin)" -ForegroundColor Green
        Start-Process $tokenUrl
    }
    
    Write-Host ""
    $token = Read-Host "Aldiginiz Tokeni buraya yapistirin ve Enter'a basin"
    if ($token -and $token.Trim() -ne "") {
        Write-Host ""
        Write-Host "Token ile yukleniyor..." -ForegroundColor Cyan
        git push https://$($token.Trim())@github.com/martinistan/vavabid.git main
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "======================================================================" -ForegroundColor Green
            Write-Host "   TEBRIKLER! REPO TOKEN ILE BASARIYLA YUKLENDI!                      " -ForegroundColor Green
            Write-Host "   Link: https://github.com/martinistan/vavabid                       " -ForegroundColor Yellow
            Write-Host "======================================================================" -ForegroundColor Green
        } else {
            Write-Host ""
            Write-Host "Yukleme basarisiz oldu. Tokenin 'repo' yetkisine sahip oldugundan emin olun." -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Kapatmak icin bir tusa basin..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
