@echo off
title DeskSyndic :5051
echo [DeskSyndic] Nettoyage port 5051...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5051" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [DeskSyndic] Suppression bin/obj...
rd /s /q "%~dp0Frontend\DeskSyndic\bin" 2>nul
rd /s /q "%~dp0Frontend\DeskSyndic\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [DeskSyndic] Build...
cd /d "%~dp0Frontend\DeskSyndic"
dotnet build
if %errorlevel% neq 0 (
    echo [DeskSyndic] BUILD FAILED
    pause
    exit /b 1
)

echo [DeskSyndic] Demarrage sur port 5051...
start "DeskSyndic :5051" dotnet run --no-build

echo [DeskSyndic] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5051/app

echo [DeskSyndic] OK !
