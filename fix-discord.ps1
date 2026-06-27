$ErrorActionPreference = "Stop"

$key = "HKCU:\Software\The Fun Pimps\7 Days To Die"
$name = "DiscordSettings_h1795906148"

# Registry: set DiscordDisabled=true
$hex = (Get-ItemProperty -Path $key -Name $name).$name
$json = [System.Text.Encoding]::UTF8.GetString([byte[]]$hex).TrimEnd("`0")
$newJson = $json -replace '"DiscordDisabled":false', '"DiscordDisabled":true'
$newBytes = [System.Text.Encoding]::UTF8.GetBytes($newJson) + @([byte]0, [byte]0)
Set-ItemProperty -Path $key -Name $name -Type Binary -Value $newBytes -Force

# AppData: DiscordUserSettings.dat (8 bytes, all zero)
[System.IO.File]::WriteAllBytes("$env:APPDATA\7DaysToDie\DiscordUserSettings.dat", [byte[]]@(0,0,0,0,0,0,0,0))

Write-Host "Discord disabled." -ForegroundColor Green
