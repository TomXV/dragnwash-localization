@echo off
rem Fallback launcher for machines where Windows SmartScreen or a security
rem policy stops the unsigned Install.exe. Opens the same installer window;
rem a console flashes for a moment, that is all.
if not exist "%~dp0installer\Installer.ps1" (
    echo The file installer\Installer.ps1 was not found next to Install.cmd.
    echo Extract the whole zip first, then run Install.cmd again.
    echo.
    pause
    exit /b 1
)
start "" powershell -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File "%~dp0installer\Installer.ps1"
