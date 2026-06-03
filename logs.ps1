$LogsDir = "$env:APPDATA\7DaysToDie\logs"
$log = Get-ChildItem "$LogsDir\output_log_client__*.txt" | Sort-Object LastWriteTime -Descending | Where-Object { $_.Length -gt 0 } | Select-Object -First 1

if ($null -eq $log) {
    Write-Host "No log files found." -ForegroundColor Red
    exit 1
}

Write-Host "=== Log: $($log.Name) ($($log.Length) bytes) ===" -ForegroundColor Cyan
Write-Host ""

$params = $args
if ($params.Count -eq 0) {
    # Show errors + last 50 lines
    Write-Host "--- ERRORS ---" -ForegroundColor Red
    Get-Content $log.FullName | Select-String -Pattern "ERR|EXC|Error|Exception" | Select-Object -Last 20
    Write-Host ""
    Write-Host "--- LAST 30 LINES ---" -ForegroundColor Yellow
    Get-Content $log.FullName -Tail 30
} elseif ($params[0] -eq "all") {
    Get-Content $log.FullName
} elseif ($params[0] -eq "tail") {
    $lines = if ($params.Count -gt 1) { [int]$params[1] } else { 50 }
    Get-Content $log.FullName -Tail $lines
} elseif ($params[0] -eq "err") {
    Get-Content $log.FullName | Select-String -Pattern "ERR|EXC|Error|Exception|NullReference|NRE"
} elseif ($params[0] -eq "cb") {
    Get-Content $log.FullName | Select-String -Pattern "\[CB\]"
} else {
    Get-Content $log.FullName | Select-String -Pattern $params[0]
}
