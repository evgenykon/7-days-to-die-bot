@echo off
echo ========================================
echo Running CompanionBot Unit Tests
echo ========================================
echo.

cd /d "F:\GameTools\ai-7d2d"

echo Building test project...
"C:\Program Files\dotnet\dotnet.exe" build CompanionBot.Tests\CompanionBot.Tests.csproj -c Test --no-dependencies

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Running tests...
"C:\Program Files\dotnet\dotnet.exe" test CompanionBot.Tests\CompanionBot.Tests.csproj -c Test --no-build --logger "console;verbosity=detailed"

echo.
echo ========================================
echo Tests completed
echo ========================================
pause
