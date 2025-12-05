@echo off
echo ====================================
echo easyWSL Debug Launcher
echo ====================================
echo.

cd /d "%~dp0easyWSL\bin\x64\Release\net8.0-windows10.0.19041.0"

echo Checking if executable exists...
if not exist "easyWSL.exe" (
    echo ERROR: easyWSL.exe not found!
    echo Please build the project first: dotnet build easyWSL.sln -c Release
    pause
    exit /b 1
)

echo Found easyWSL.exe
echo.
echo Launching application...
echo (This window will show any error messages)
echo.

easyWSL.exe

echo.
echo ====================================
echo Application exited with code: %ERRORLEVEL%
echo ====================================
echo.

if %ERRORLEVEL% NEQ 0 (
    echo The application crashed or encountered an error.
    echo Error code: %ERRORLEVEL%
    echo.
    echo Common fixes:
    echo 1. Install .NET 8.0 Desktop Runtime from:
    echo    https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo 2. Install Windows App SDK 1.5 from:
    echo    https://aka.ms/windowsappsdk
    echo.
    echo 3. Check Windows Event Viewer for details:
    echo    eventvwr.msc ^> Windows Logs ^> Application
)

pause