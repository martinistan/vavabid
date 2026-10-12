@echo off
title Umutistan Vavabid - GitHub Push
:loop
powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0push_to_github.ps1"
echo.
echo ======================================================================
echo   Pencere otomatik KAPANMAYACAK sekilde ayarlandi.
echo   Yeniden calistirmak icin bir tusa basin...
echo ======================================================================
pause
goto loop
