<#
    decompile-quicksave.ps1

    Decompiles the installed Quick_Save plugin DLL to a single C# file using the
    workspace AtoDecompile tool (ILSpy engine as a library). Read-only w.r.t. the game.

        cd "C:\Users\dhelm\source\repos\GameMods\AcrossTheObelisk"
        powershell -ExecutionPolicy Bypass -File .\scripts\decompile-quicksave.ps1
#>
$ErrorActionPreference = 'Stop'

$repo    = Split-Path -Parent $PSScriptRoot
$profile = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default'
$dll     = Join-Path $profile 'BepInEx\plugins\Binbin-Quick_Save\com.binbin.quicksave.dll'
$out     = Join-Path $repo 'refs\QuickSave-src\com.binbin.quicksave.cs'

# Extra search dirs so the decompiler can resolve game/framework references.
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed'
$core    = Join-Path $profile 'BepInEx\core'
$ess     = Join-Path $profile 'BepInEx\plugins\meds-Obeliskial_Essentials'
$content = Join-Path $profile 'BepInEx\plugins\meds-Obeliskial_Content'

if (-not (Test-Path $dll)) { Write-Error "Quick_Save dll not found:`n  $dll"; return }

$proj = Join-Path $repo 'tools\AtoDecompile'
Write-Host "Decompiling Quick_Save via AtoDecompile (first run restores NuGet, may take a moment)...`n"
dotnet run --project $proj -c Release -- $dll $out $managed $core $ess $content

if (Test-Path $out) {
    Write-Host "`nOutput file:" -ForegroundColor Green
    Get-Item $out | Select-Object FullName, Length, LastWriteTime | Format-List
} else {
    Write-Host "Decompile did not produce output; see messages above." -ForegroundColor Yellow
}
