@echo off
setlocal
cd /d "%~dp0"
title Neural Pipeline Studio v2.0 Launcher

:: 1. Check if executable exists in current folder
if exist "%~dp0NeuralPipelineStudio.exe" (
    start "" "%~dp0NeuralPipelineStudio.exe"
    exit /b 0
)

:: 2. Check if portable package exe exists in dist
if exist "%~dp0dist\NeuralPipelineStudio_v2.0_Portable\NeuralPipelineStudio.exe" (
    start "" "%~dp0dist\NeuralPipelineStudio_v2.0_Portable\NeuralPipelineStudio.exe"
    exit /b 0
)

:: 3. Check if standard dist exe exists
if exist "%~dp0dist\NeuralPipelineStudio.exe" (
    start "" "%~dp0dist\NeuralPipelineStudio.exe"
    exit /b 0
)

:: 4. Check if Release build exists in bin
if exist "%~dp0src\NeuralPipelineStudio\bin\Release\net8.0-windows\win-x64\NeuralPipelineStudio.exe" (
    start "" "%~dp0src\NeuralPipelineStudio\bin\Release\net8.0-windows\win-x64\NeuralPipelineStudio.exe"
    exit /b 0
)

:: 5. If not compiled yet, try building/running with dotnet SDK
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    echo ==========================================================
    echo  Neural Pipeline Studio - First Time Build ^& Launch
    echo ==========================================================
    echo Building and starting Neural Pipeline Studio with .NET 8...
    dotnet run --project "%~dp0src\NeuralPipelineStudio\NeuralPipelineStudio.csproj" -c Release
    exit /b 0
)

:: 6. Fallback: Prompt user to download pre-compiled portable package from GitHub Releases
echo ========================================================================
echo  [ERROR] NeuralPipelineStudio.exe not found!
echo ========================================================================
echo.
echo You have downloaded the source code repository instead of the pre-compiled
echo portable release.
echo.
echo To run the app immediately with zero configuration:
echo 1. Download NeuralPipelineStudio_v2.0_Portable.zip from GitHub Releases:
echo    https://github.com/brescale/NeuralPipelineStudio/releases/tag/v2.0-beta
echo.
echo Opening the GitHub Releases download page now...
start https://github.com/brescale/NeuralPipelineStudio/releases/tag/v2.0-beta
echo.
pause
exit /b 1
