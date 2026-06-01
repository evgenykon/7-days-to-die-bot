@echo off
setlocal

set "PROPS_FILE=%~dp0..\Directory.Build.props"

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

echo Game path: %GAME_PATH%
echo.

if not exist "%GAME_PATH%\7DaysToDie.exe" (
    echo ERROR: 7DaysToDie.exe not found at %GAME_PATH%
    echo Check GamePath in Directory.Build.props
    pause
    exit /b 1
)

echo Building CompanionBot mod...
echo.

dotnet build "%~dp0CompanionBot.csproj" -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo Mod installed to: %GAME_PATH%\Mods\CompanionBot
echo.
echo To use the mod:
echo 1. Disable EAC (Easy Anti-Cheat)
echo 2. Start the game
echo 3. Open console (F1)
echo 4. Type: spawnentity companionbot
echo    Or: spawnentity companionbotarmed
echo.
pause
