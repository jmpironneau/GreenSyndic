@echo off
echo [GreenSyndic] Killing old backend instances...
taskkill /F /IM GreenSyndic.Api.exe 2>nul
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5050" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul

echo [GreenSyndic] Cleaning build output...
rd /s /q "%~dp0Backend\src\GreenSyndic.Api\bin" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Api\obj" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Core\bin" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Core\obj" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Services\bin" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Services\obj" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Infrastructure\bin" 2>nul
rd /s /q "%~dp0Backend\src\GreenSyndic.Infrastructure\obj" 2>nul

timeout /t 3 /nobreak >nul

echo [GreenSyndic] Building backend...
cd /d "%~dp0Backend"
dotnet build src\GreenSyndic.Api
if %errorlevel% neq 0 (
    echo [GreenSyndic] BUILD FAILED
    pause
    exit /b 1
)

echo [GreenSyndic] Starting backend on port 5050...
start "GreenSyndic Backend" dotnet run --project src\GreenSyndic.Api --no-build

echo [GreenSyndic] Opening browser in 10 seconds...
timeout /t 10 /nobreak >nul
start chrome http://localhost:5050

echo [GreenSyndic] Done!
