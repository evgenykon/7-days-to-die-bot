$logDir = "$env:APPDATA\7DaysToDie\Logs"
$log = Get-ChildItem "$logDir" -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($log -eq $null) {
    Write-Host "No logs found" -ForegroundColor Red
    exit 1
}

Write-Host "=== Latest log: $($log.Name)" -ForegroundColor Cyan
Write-Host ""

$content = Get-Content $log.FullName

Write-Host "--- CompanionBot ---" -ForegroundColor Yellow
$content | Select-String -Pattern "CompanionBot"

Write-Host ""
Write-Host "--- ERR / Exception / Failed ---" -ForegroundColor Yellow
$content | Select-String -Pattern "ERR|Exception|Failed"
