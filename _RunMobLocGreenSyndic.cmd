@echo off
title MobLoc :5054
echo [MobLoc] Nettoyage port 5054...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5054" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [MobLoc] Suppression bin/obj...
rd /s /q "%~dp0Frontend\MobLoc\bin" 2>nul
rd /s /q "%~dp0Frontend\MobLoc\obj" 2>nul
timeout /t 2 /nobreak >nul

echo [MobLoc] Build...
cd /d "%~dp0Frontend\MobLoc"
dotnet build
if %errorlevel% neq 0 (
    echo [MobLoc] BUILD FAILED
    pause
    exit /b 1
)

echo [MobLoc] Demarrage sur port 5054...
start "MobLoc :5054" dotnet run --no-build

echo [MobLoc] Ouverture navigateur dans 6 secondes...
timeout /t 6 /nobreak >nul
start chrome http://localhost:5054/app

echo [MobLoc] OK !
