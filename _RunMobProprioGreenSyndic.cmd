@echo off
title MobProprio :5053
echo [MobProprio] Nettoyage port 5053...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5053" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [MobProprio] Suppression bin/obj...
rd /s /q "%~dp0Frontend\MobProprio\bin" 2>nul
rd /s /q "%~dp0Frontend\MobProprio\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [MobProprio] Build...
cd /d "%~dp0Frontend\MobProprio"
dotnet build
if %errorlevel% neq 0 (
    echo [MobProprio] BUILD FAILED
    pause
    exit /b 1
)

echo [MobProprio] Demarrage sur port 5053...
start "MobProprio :5053" dotnet run --no-build

echo [MobProprio] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5053/app

echo [MobProprio] OK !
