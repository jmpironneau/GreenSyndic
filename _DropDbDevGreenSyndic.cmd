@echo off
echo [GreenSyndic] Dropping database greensyndic_dev...
echo.
set /p CONFIRM="Are you sure you want to drop the database? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo [GreenSyndic] Cancelled.
    exit /b
)
set PGUSER=jmp
set PGPASSWORD=Piro2026!
set PGHOST=localhost
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -c "DROP DATABASE IF EXISTS greensyndic_dev;" postgres
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [GreenSyndic] Database dropped successfully!
    echo [GreenSyndic] Restart the backend to recreate it, then run Seed.
) else (
    echo.
    echo [GreenSyndic] ERROR: Failed to drop database.
    echo [GreenSyndic] Make sure PostgreSQL is running and no app is connected.
    echo.
    pause
    exit /b 1
)
