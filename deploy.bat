@echo off
setlocal enabledelayedexpansion

set "PROPS_FILE=%~dp0Directory.Build.props"
set "SOURCE_DIR=%~dp0CompanionBot"
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

set "MOD_TARGET=%GAME_PATH%\Mods\%MOD_NAME%"

echo ============================================
echo   Deploying CompanionBot Mod
echo ============================================
echo Game:   %GAME_PATH%
echo Target: %MOD_TARGET%
echo Source: %SOURCE_DIR%
echo.

if not exist "%MOD_TARGET%" (
    echo ERROR: Target folder does not exist. Run install.bat first.
    pause
    exit /b 1
)

echo [1/3] Copying DLL...
copy /Y "%SOURCE_DIR%\bin\Release\net48\%MOD_NAME%.dll" "%MOD_TARGET%\" >nul
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Could not copy DLL. Is the game running? Close the game and try again.
) else (
    echo   OK - CompanionBot.dll copied
)

echo [2/3] Copying Config...
if not exist "%MOD_TARGET%\Config" mkdir "%MOD_TARGET%\Config"
xcopy /E /I /Y "%SOURCE_DIR%\Config" "%MOD_TARGET%\Config" >nul
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Config copy failed
) else (
    echo   OK - Config copied
)

echo [3/3] Copying ModInfo.xml...
copy /Y "%SOURCE_DIR%\ModInfo.xml" "%MOD_TARGET%\" >nul
echo   OK - ModInfo.xml copied

echo.
echo ============================================
echo   Deploy complete!
echo ============================================
echo.
echo Check logs: %%APPDATA%%\7DaysToDie\Logs\output_log*.txt
echo Look for: "[CompanionBot]" or "ERR" or "Failed"
echo.
pause
