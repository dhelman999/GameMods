<#
    gather-game-info.ps1

    Read-only. Collects the info needed to judge mod/game compatibility:
      - Steam build id + last-updated for Across the Obelisk (appid 1385380)
      - File versions of the game exe and UnityPlayer.dll
      - Versions of mods currently installed in the active TMM profile (from mods.yml)

    Safe to run any time (no changes made). Admin not required.
        cd "C:\Users\dhelm\source\repos\GameMods\AcrossTheObelisk"
        powershell -ExecutionPolicy Bypass -File .\scripts\gather-game-info.ps1
#>

$ErrorActionPreference = 'SilentlyContinue'

$gameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk'
$acf     = 'C:\Program Files (x86)\Steam\steamapps\appmanifest_1385380.acf'

Write-Host "==================== STEAM APP MANIFEST ====================" -ForegroundColor Cyan
if (Test-Path $acf) {
    (Get-Content $acf | Select-String -Pattern '"name"|"buildid"|"LastUpdated"|"StateFlags"|"installdir"|"SizeOnDisk"') `
        | ForEach-Object { $_.Line.Trim() }
} else {
    Write-Host "appmanifest_1385380.acf not found at default path."
    Write-Host "Searching other Steam libraries..."
    $vdf = 'C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf'
    if (Test-Path $vdf) { Get-Content $vdf }
}

Write-Host "`n==================== GAME FILE VERSIONS ====================" -ForegroundColor Cyan
foreach ($f in @('AcrossTheObelisk.exe','UnityPlayer.dll','AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll')) {
    $p = Join-Path $gameDir $f
    if (Test-Path $p) {
        $vi = (Get-Item $p).VersionInfo
        $lw = (Get-Item $p).LastWriteTime
        "{0,-45} FileVersion={1,-18} ProductVersion={2,-18} Modified={3}" -f $f, $vi.FileVersion, $vi.ProductVersion, $lw
    } else {
        "{0,-45} (missing)" -f $f
    }
}

Write-Host "`n==================== TMM PROFILES / mods.yml ====================" -ForegroundColor Cyan
$profRoot = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles'
if (Test-Path $profRoot) {
    Get-ChildItem $profRoot -Directory | ForEach-Object {
        $prof = $_.Name
        $yml  = Join-Path $_.FullName 'mods.yml'
        Write-Host "`n--- Profile: $prof ---"
        if (Test-Path $yml) {
            $lines = Get-Content $yml
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match '^\s*-?\s*name:\s*(.+)$') {
                    $name = $Matches[1].Trim()
                    $maj=$min=$pat=$null; $enabled=$null
                    for ($j = $i+1; $j -lt [Math]::Min($i+20,$lines.Count); $j++) {
                        if ($lines[$j] -match '^\s*-?\s*name:\s*') { break }
                        if ($lines[$j] -match 'major:\s*(\d+)') { $maj=$Matches[1] }
                        if ($lines[$j] -match 'minor:\s*(\d+)') { $min=$Matches[1] }
                        if ($lines[$j] -match 'patch:\s*(\d+)') { $pat=$Matches[1] }
                        if ($lines[$j] -match 'enabled:\s*(\w+)') { $enabled=$Matches[1] }
                    }
                    if ($maj -ne $null) { "  {0,-45} {1}.{2}.{3}  enabled={4}" -f $name, $maj, $min, $pat, $enabled }
                }
            }
        } else { Write-Host "  (no mods.yml - profile empty?)" }
    }
    Write-Host "`n--- Installed plugin folders (active profile 'Default') ---"
    $plug = Join-Path $profRoot 'Default\BepInEx\plugins'
    if (Test-Path $plug) { Get-ChildItem $plug -Directory | ForEach-Object { "  $($_.Name)" } } else { Write-Host "  (no plugins folder)" }
} else {
    Write-Host "No TMM AcrossTheObelisk profiles found."
}
Write-Host "`nDone."
