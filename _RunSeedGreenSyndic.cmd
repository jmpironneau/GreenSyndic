@echo off
title Seed :5055
echo [Seed] Nettoyage port 5055...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5055" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [Seed] Suppression bin/obj...
rd /s /q "%~dp0Frontend\Seed\bin" 2>nul
rd /s /q "%~dp0Frontend\Seed\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [Seed] Build...
cd /d "%~dp0Frontend\Seed"
dotnet build
if %errorlevel% neq 0 (
    echo [Seed] BUILD FAILED
    pause
    exit /b 1
)

echo [Seed] Demarrage sur port 5055...
start "Seed :5055" dotnet run --no-build

echo [Seed] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5055

echo [Seed] OK !
