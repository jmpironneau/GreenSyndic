@echo off
title MobSyndic :5052
echo [MobSyndic] Nettoyage port 5052...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5052" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [MobSyndic] Suppression bin/obj...
rd /s /q "%~dp0Frontend\MobSyndic\bin" 2>nul
rd /s /q "%~dp0Frontend\MobSyndic\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [MobSyndic] Build...
cd /d "%~dp0Frontend\MobSyndic"
dotnet build
if %errorlevel% neq 0 (
    echo [MobSyndic] BUILD FAILED
    pause
    exit /b 1
)

echo [MobSyndic] Demarrage sur port 5052...
start "MobSyndic :5052" dotnet run --no-build

echo [MobSyndic] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5052/app

echo [MobSyndic] OK !
