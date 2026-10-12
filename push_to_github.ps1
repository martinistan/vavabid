# UTF-8 encoded PowerShell script for GitHub push
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Umutistan Vavabid - GitHub Push"

Clear-Host
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "   UMUTISTAN DOLANDIRICILIK HIZMETLERI (VAVABID) - GITHUB PUSH        " -ForegroundColor Yellow
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Hedef Depo: https://github.com/martinistan/vavabid" -ForegroundColor Gray
Write-Host "Dal       : main" -ForegroundColor Gray
Write-Host ""

$repoPath = "c:\Users\musta\OneDrive\Desktop\umutistandolandırıcılıkhizmetleri"
Set-Location -LiteralPath $repoPath

Write-Host "GitHub'a yukleniyor..." -ForegroundColor Green
Write-Host "(Eger tarayici acilirsa 'Sign in with your browser' ile onaylayin)" -ForegroundColor Gray
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
    Write-Host "   Tarayici ile giris acilmadiysa Personal Access Token kullanabilirsiniz." -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Red
    Write-Host ""
    $token = Read-Host "GitHub Personal Access Tokeninizi yapistirin (veya cikmak icin Enter)"
    if ($token -and $token.Trim() -ne "") {
        Write-Host ""
        Write-Host "Token ile yukleniyor..." -ForegroundColor Cyan
        git push https://$($token.Trim())@github.com/martinistan/vavabid.git main
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "======================================================================" -ForegroundColor Green
            Write-Host "   TOKEN ILE BASARIYLA YUKLENDI!                                      " -ForegroundColor Green
            Write-Host "   Link: https://github.com/martinistan/vavabid                       " -ForegroundColor Yellow
            Write-Host "======================================================================" -ForegroundColor Green
        } else {
            Write-Host "Yukleme basarisiz. Token izinlerini kontrol edin (repo secili olmali)." -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Cikmak icin bir tusa basin..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
