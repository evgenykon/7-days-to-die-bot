$GamePath = "F:\SteamLibrary\steamapps\common\7 Days To Die"
$ModDir = "$GamePath\Mods\CompanionBot"
$ConfigDir = "$ModDir\Config"
$SrcDir = "F:\GameTools\ai-7d2d\src\CompanionBot"

Write-Host "=== Deploying CompanionBot ===" -ForegroundColor Cyan

# Build
Write-Host "[1/3] Building..." -NoNewline
dotnet build "$SrcDir\CompanionBot.csproj" -nologo -v q 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAILED" -ForegroundColor Red
    dotnet build "$SrcDir\CompanionBot.csproj" 2>&1
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# Create dirs
Write-Host "[2/3] Creating directories..." -NoNewline
New-Item -ItemType Directory -Path $ModDir -Force | Out-Null
New-Item -ItemType Directory -Path $ConfigDir -Force | Out-Null
Write-Host " OK" -ForegroundColor Green

# Copy files
Write-Host "[3/3] Copying files..."
try {
    Copy-Item "$SrcDir\bin\Debug\net48\CompanionBot.dll" -Destination "$ModDir\CompanionBot.dll" -Force -ErrorAction Stop
    Write-Host "  DLL: OK" -ForegroundColor Green
} catch {
    Write-Host "  DLL: SKIP (game running, file in use)" -ForegroundColor Yellow
}

Copy-Item "$SrcDir\ModInfo.xml" -Destination "$ModDir\ModInfo.xml" -Force
Write-Host "  ModInfo.xml: OK" -ForegroundColor Green

Copy-Item "$SrcDir\Config\entityclasses.xml" -Destination "$ConfigDir\entityclasses.xml" -Force
Write-Host "  entityclasses.xml: OK" -ForegroundColor Green

Copy-Item "$SrcDir\Config\entitygroups.xml" -Destination "$ConfigDir\entitygroups.xml" -Force
Write-Host "  entitygroups.xml: OK" -ForegroundColor Green

Write-Host "=== Done ===" -ForegroundColor Cyan
