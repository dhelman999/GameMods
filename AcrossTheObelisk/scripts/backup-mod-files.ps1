<#
    backup-mod-files.ps1

    Moves leftover mod artifacts out of the Across the Obelisk game directory into a
    timestamped backup folder inside this workspace, so the game folder is a clean,
    vanilla install before reinstalling mods via Thunderstore Mod Manager.

    - Only MOVES known mod/BepInEx artifacts (never touches real game files).
    - Nothing is deleted; everything is moved to a backup you can restore or delete later.

    RUN AS ADMINISTRATOR (the game lives under "Program Files (x86)").
    Right-click PowerShell -> "Run as administrator", then:
        cd "C:\Users\dhelm\source\repos\GameMods\AcrossTheObelisk"
        powershell -ExecutionPolicy Bypass -File .\scripts\backup-mod-files.ps1
#>

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$gameDir   = 'C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk'
$stamp     = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDir = Join-Path (Join-Path $projectRoot 'backups') "gamedir-modfiles-$stamp"

# Known BepInEx + Obeliskial mod artifacts that a manual/TMM install leaves in the game folder.
# These are NOT part of the vanilla game, so moving them yields a clean install.
$modArtifacts = @(
    'BepInEx',                 # plugin loader + all plugins/config/logs
    'doorstop_libs',           # doorstop injector libs
    'dotnet',                  # only present for BepInEx 6 (harmless if absent)
    'mono',                    # occasionally bundled by BepInEx
    'Obeliskial_importing',    # Obeliskial Content custom-content import folder
    'Obeliskial_exported',     # Obeliskial exported data folder
    'doorstop_config.ini',
    '.doorstop_version',
    'winhttp.dll',             # doorstop proxy dll that bootstraps BepInEx
    'run_bepinex.sh',
    'changelog.txt'            # BepInEx changelog (game has no such file)
)

Write-Host "Game dir : $gameDir"
Write-Host "Backup   : $backupDir`n"

if (-not (Test-Path -LiteralPath $gameDir)) {
    Write-Error "Game directory not found: $gameDir"
    return
}

$found = @()
foreach ($name in $modArtifacts) {
    $path = Join-Path $gameDir $name
    if (Test-Path -LiteralPath $path) { $found += $name }
}

if ($found.Count -eq 0) {
    Write-Host "No known mod artifacts found in the game directory. It already looks clean." -ForegroundColor Green
    Write-Host "`nFull game-dir listing for your review:"
    Get-ChildItem -LiteralPath $gameDir -Force | Select-Object Mode, LastWriteTime, Length, Name | Format-Table -AutoSize
    return
}

Write-Host "Found these mod artifacts to back up:" -ForegroundColor Yellow
$found | ForEach-Object { Write-Host "  - $_" }

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

foreach ($name in $found) {
    $src = Join-Path $gameDir $name
    $dst = Join-Path $backupDir $name
    Write-Host "Moving $name ..." -NoNewline
    Move-Item -LiteralPath $src -Destination $dst -Force
    Write-Host " done."
}

Write-Host "`nBacked up $($found.Count) item(s) to:`n  $backupDir" -ForegroundColor Green
Write-Host "`nRemaining game-dir contents (should now be vanilla):"
Get-ChildItem -LiteralPath $gameDir -Force | Select-Object Mode, LastWriteTime, Length, Name | Format-Table -AutoSize
