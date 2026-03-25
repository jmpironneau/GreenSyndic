@echo off
title GreenSyndic Backend :5050
echo [Backend] Nettoyage port 5050...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5050" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [Backend] Suppression bin/obj...
rd /s /q "%~dp0Backend\GreenSyndic.Api\bin" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Api\obj" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Core\bin" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Core\obj" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Services\bin" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Services\obj" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Infrastructure\bin" 2>nul
rd /s /q "%~dp0Backend\GreenSyndic.Infrastructure\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [Backend] Build...
cd /d "%~dp0Backend\GreenSyndic.Api"
dotnet build
if %errorlevel% neq 0 (
    echo [Backend] BUILD FAILED
    pause
    exit /b 1
)

echo [Backend] Demarrage sur port 5050...
start "GreenSyndic Backend :5050" dotnet run --no-build

echo [Backend] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5050/index.html

echo [Backend] OK !
