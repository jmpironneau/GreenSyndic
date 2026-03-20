@echo off
echo ============================================================
echo  GreenSyndic - Lancement de tous les tests
echo ============================================================
echo.

:: Kill any running backend that could lock DLLs
taskkill /F /IM GreenSyndic.Api.exe 2>nul
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5050" ^| findstr "LISTENING"') do taskkill /F /PID %%a 2>nul
timeout /t 2 /nobreak >nul

cd /d "%~dp0Backend"

:: =============================================
::  1. Tests unitaires / integration (NUnit)
:: =============================================
echo.
echo [1/2] Tests unitaires et integration (NUnit)...
echo ------------------------------------------------------------
dotnet test tests\GreenSyndic.Tests --verbosity normal
set UNIT_RESULT=%errorlevel%

if %UNIT_RESULT% neq 0 (
    echo.
    echo [ECHEC] Tests unitaires / integration : des tests ont echoue.
) else (
    echo.
    echo [OK] Tests unitaires / integration : tous les tests passent.
)

:: =============================================
::  2. Tests visuels / E2E (placeholder)
:: =============================================
echo.
echo [2/2] Tests visuels / E2E...
echo ------------------------------------------------------------

:: TODO: ajouter ici les tests visuels quand le frontend sera en place
:: Exemples :
::   npx playwright test
::   npx cypress run
::   dotnet test tests\GreenSyndic.E2E
echo   (aucun test visuel configure pour le moment)
echo   Ajouter Playwright ou Cypress dans cette section.
echo.

:: =============================================
::  Resume
:: =============================================
echo ============================================================
if %UNIT_RESULT% neq 0 (
    echo  RESULTAT : ECHEC - voir les details ci-dessus
    echo ============================================================
    pause
    exit /b 1
)

echo  RESULTAT : TOUS LES TESTS PASSENT
echo ============================================================
pause
