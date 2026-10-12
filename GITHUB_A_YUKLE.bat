@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist ".git" cd /d "c:\Users\musta\OneDrive\Desktop\umutistandolandırıcılıkhizmetleri"
cls
echo ======================================================================
echo   🦝 UMUTISTAN DOLANDIRICILIK HIZMETLERI (VAVABID) - GITHUB PUSH
echo ======================================================================
echo.
echo Depo: https://github.com/martinistan/vavabid
echo Dal : main
echo.
echo GitHub'a gonderiliyor...
echo (Tarayici penceresi acilirsa 'Sign in with your browser' butonuna tiklayin)
echo.
git push -u origin main
if %ERRORLEVEL% equ 0 (
    echo.
    echo ======================================================================
    echo   ✅ TEBRIKLER! REPO BASARIYLA GITHUB'A YUKLENDI!
    echo   👉 https://github.com/martinistan/vavabid
    echo ======================================================================
    goto son
)

echo.
echo ======================================================================
echo   ⚠️ GIT GIRISI GEREKIYOR
echo   Tarayici ile giris yapamadiysaniz bir Personal Access Token (PAT)
echo   kullanarak tek seferde yukleyebilirsiniz.
echo ======================================================================
echo.
set /p GHTOKEN="GitHub Tokeninizi yapistirin (veya cikmak icin Enter): "
if not "%GHTOKEN%"=="" (
    echo.
    echo Token ile gonderiliyor...
    git push https://%GHTOKEN%@github.com/martinistan/vavabid.git main
    if %ERRORLEVEL% equ 0 (
        echo.
        echo ======================================================================
        echo   ✅ TOKEN ILE BASARIYLA YUKLENDI!
        echo   👉 https://github.com/martinistan/vavabid
        echo ======================================================================
    ) else (
        echo.
        echo ❌ Yukleme basarisiz oldu. Token yetkilerini kontrol edin (repo secili olmali).
    )
)

:son
echo.
pause
