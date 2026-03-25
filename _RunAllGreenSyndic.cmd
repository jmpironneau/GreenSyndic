@echo off
title GreenSyndic - Toutes les apps
echo ============================================================
echo  GreenSyndic - Lancement de toutes les applications
echo ============================================================
echo.

echo [1/8] Nettoyage de tous les ports...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5050 :5051 :5052 :5053 :5054 :5055" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul
timeout /t 2 /nobreak >nul

echo [2/8] Build Backend...
cd /d "%~dp0Backend"
dotnet build GreenSyndic.slnx --verbosity quiet
if %errorlevel% neq 0 (
    echo [ECHEC] BUILD BACKEND FAILED
    pause
    exit /b 1
)

echo [3/8] Build Frontend...
cd /d "%~dp0Frontend"
dotnet build Frontend.slnx --verbosity quiet
if %errorlevel% neq 0 (
    echo [ECHEC] BUILD FRONTEND FAILED
    pause
    exit /b 1
)

echo.
echo [4/8] Backend :5050...
cd /d "%~dp0Backend\GreenSyndic.Api"
start "GreenSyndic Backend :5050" dotnet run --no-build

echo [5/8] DeskSyndic :5051...
cd /d "%~dp0Frontend\DeskSyndic"
start "DeskSyndic :5051" dotnet run --no-build

echo [6/8] MobSyndic :5052...
cd /d "%~dp0Frontend\MobSyndic"
start "MobSyndic :5052" dotnet run --no-build

echo [7/8] MobProprio :5053...
cd /d "%~dp0Frontend\MobProprio"
start "MobProprio :5053" dotnet run --no-build

echo [8/8] MobLoc :5054...
cd /d "%~dp0Frontend\MobLoc"
start "MobLoc :5054" dotnet run --no-build

echo.
echo Attente 12 secondes...
timeout /t 12 /nobreak >nul

echo Ouverture Chrome...
start chrome http://localhost:5050/index.html
start chrome http://localhost:5051/app
start chrome "http://localhost:5052/app" --window-size=393,852
start chrome "http://localhost:5053/app" --window-size=393,852
start chrome "http://localhost:5054/app" --window-size=393,852

echo.
echo ============================================================
echo  Toutes les apps:
echo    Backend    : http://localhost:5050
echo    DeskSyndic : http://localhost:5051/app
echo    MobSyndic  : http://localhost:5052/app
echo    MobProprio : http://localhost:5053/app
echo    MobLoc     : http://localhost:5054/app
echo    Seed       : http://localhost:5055 (lancer separement)
echo ============================================================
