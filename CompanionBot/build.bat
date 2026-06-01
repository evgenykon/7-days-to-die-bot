@echo off
echo Building CompanionBot mod...
echo.

dotnet build -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo Mod installed to: F:\SteamLibrary\steamapps\common\7 Days To Die\Mods\CompanionBot
echo.
echo To use the mod:
echo 1. Disable EAC (Easy Anti-Cheat)
echo 2. Start the game
echo 3. Open console (F1)
echo 4. Type: spawnentity companionbot
echo    Or: spawnentity companionbotarmed
echo.
pause
