<#
    grab-log.ps1
    Copies the active TMM profile's BepInEx log into .\diagnostics with a timestamp,
    so it can be reviewed/shared. Read-only w.r.t. the game; only copies.

        cd "C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods"
        powershell -ExecutionPolicy Bypass -File .\scripts\grab-log.ps1
#>
$ErrorActionPreference = 'SilentlyContinue'

$profile = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default'
$log     = Join-Path $profile 'BepInEx\LogOutput.log'
$destDir = 'C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods\diagnostics'
$stamp   = Get-Date -Format 'yyyyMMdd-HHmmss'
$dest    = Join-Path $destDir "LogOutput-$stamp.log"

New-Item -ItemType Directory -Force -Path $destDir | Out-Null

if (Test-Path $log) {
    Copy-Item $log $dest -Force
    $fi = Get-Item $dest
    Write-Host "Copied log -> $dest" -ForegroundColor Green
    Write-Host ("Size: {0} bytes, source last modified: {1}" -f $fi.Length, (Get-Item $log).LastWriteTime)
    Write-Host "`n---- Errors / warnings in this log ----" -ForegroundColor Cyan
    Select-String -Path $log -Pattern '\[Error|\[Warning|Loading \[|has loaded|not installed|Exception' |
        ForEach-Object { $_.Line }
} else {
    Write-Host "No log found at: $log" -ForegroundColor Yellow
    Write-Host "Launch the game via TMM at least once, then rerun this."
}
