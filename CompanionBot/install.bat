@echo off
setlocal enabledelayedexpansion

set "PROPS_FILE=%~dp0..\Directory.Build.props"
set "MOD_SOURCE=%~dp0"
set "MOD_NAME=CompanionBot"

if not exist "%PROPS_FILE%" (
    echo ERROR: Directory.Build.props not found!
    echo Copy Directory.Build.props.example to Directory.Build.props and set your game path.
    pause
    exit /b 1
)

for /f "tokens=* usebackq" %%a in (`powershell -NoProfile -Command "([xml](Get-Content '%PROPS_FILE%')).Project.PropertyGroup.GamePath"`) do set "GAME_PATH=%%a"

if "%GAME_PATH%"=="" (
    echo ERROR: GamePath not set in Directory.Build.props
    pause
    exit /b 1
)

if not exist "%GAME_PATH%\7DaysToDie.exe" (
    echo ERROR: 7DaysToDie.exe not found at %GAME_PATH%
    echo Check GamePath in Directory.Build.props
    pause
    exit /b 1
)

set "MOD_TARGET=%GAME_PATH%\Mods\%MOD_NAME%"

echo ============================================
echo   CompanionBot Mod Installer
echo ============================================
echo.
echo Game path:    %GAME_PATH%
echo Mod source:   %MOD_SOURCE%
echo Mod target:   %MOD_TARGET%
echo.

echo [1/5] Creating mod directory...
if not exist "%MOD_TARGET%" mkdir "%MOD_TARGET%"

echo [2/5] Copying ModInfo.xml...
copy /Y "%MOD_SOURCE%ModInfo.xml" "%MOD_TARGET%\" >nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy ModInfo.xml
    pause
    exit /b 1
)

echo [3/5] Copying Config files...
if not exist "%MOD_TARGET%\Config" mkdir "%MOD_TARGET%\Config"
xcopy /E /I /Y "%MOD_SOURCE%Config" "%MOD_TARGET%\Config" >nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy Config files
    pause
    exit /b 1
)

echo [4/5] Creating Data directory for RAG memories...
if not exist "%MOD_TARGET%\Data" mkdir "%MOD_TARGET%\Data"

echo [5/5] Building and copying DLL...
if exist "%MOD_SOURCE%bin\Release\net48\%MOD_NAME%.dll" (
    copy /Y "%MOD_SOURCE%bin\Release\net48\%MOD_NAME%.dll" "%MOD_TARGET%\" >nul
) else (
    echo WARNING: DLL not found. Run 'dotnet build -c Release' first.
)

echo.
echo ============================================
echo   Installation complete!
echo ============================================
echo.
echo Mod installed to: %MOD_TARGET%
echo.
echo Files installed:
dir /B "%MOD_TARGET%"
echo.
echo Next steps:
echo   1. Disable EAC (Easy Anti-Cheat)
echo   2. Start the game
echo   3. Open console (F1)
echo   4. Type: spawnentity companionbot
echo.
pause
