@echo off
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell Start-Process -FilePath '%~f0' -Verb RunAs
    exit /b
)
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -NoProfile -File "Scripts\_AllDeploy.ps1"
pause
