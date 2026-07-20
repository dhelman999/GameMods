<#
    backup-mod-files-round2.ps1

    Second cleanup pass. Removes the older MANUAL-install leftovers and mod-created
    save files that remained in the Across the Obelisk game directory after round 1.

    - Only MOVES (never deletes). Everything goes to a timestamped backup in this workspace.
    - Uses explicit name patterns that can only be mod artifacts (Thunderstore package
      folders/zips, the .thunderstoremm marker, and Quick_Save's gamedata_*.ato turn saves).

    RUN AS ADMINISTRATOR:
        cd "C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods"
        powershell -ExecutionPolicy Bypass -File .\scripts\backup-mod-files-round2.ps1
#>

$ErrorActionPreference = 'Stop'

$gameDir   = 'C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk'
$stamp     = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDir = Join-Path 'C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods\backups' "gamedir-modfiles-$stamp"

# Name patterns that are unambiguously mod artifacts (never part of the vanilla game).
$patterns = @(
    'BepInEx-*',            # Thunderstore BepInEx pack folder/zip (Author-Package-Version)
    'Binbin-*',             # Quick_Save (and any other Binbin) package folder/zip
    'meds-*',               # Obeliskial Essentials/Content packages, if present
    '.thunderstoremm',      # Thunderstore Mod Manager marker file
    'gamedata_*.ato'        # Quick_Save mid-combat turn saves dumped into game root
)

Write-Host "Game dir : $gameDir"
Write-Host "Backup   : $backupDir`n"

if (-not (Test-Path -LiteralPath $gameDir)) { Write-Error "Game directory not found: $gameDir"; return }

# Collect matches (unique), excluding anything that isn't clearly a mod artifact.
$items = @()
foreach ($pat in $patterns) {
    $items += Get-ChildItem -LiteralPath $gameDir -Force -Filter $pat -ErrorAction SilentlyContinue
}
$items = $items | Sort-Object FullName -Unique

if ($items.Count -eq 0) {
    Write-Host "No matching leftover mod artifacts found. Game folder already clean." -ForegroundColor Green
} else {
    Write-Host "Found these leftovers to back up:" -ForegroundColor Yellow
    $items | ForEach-Object { Write-Host ("  - {0,-45} {1}" -f $_.Name, $_.Mode) }

    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    foreach ($it in $items) {
        Write-Host "Moving $($it.Name) ..." -NoNewline
        Move-Item -LiteralPath $it.FullName -Destination (Join-Path $backupDir $it.Name) -Force
        Write-Host " done."
    }
    Write-Host "`nBacked up $($items.Count) item(s) to:`n  $backupDir" -ForegroundColor Green
}

Write-Host "`nFinal game-dir contents (should now be fully vanilla):"
Get-ChildItem -LiteralPath $gameDir -Force | Select-Object Mode, LastWriteTime, Length, Name | Format-Table -AutoSize
