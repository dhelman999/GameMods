<#
    decompile-game-types.ps1

    Decompiles specific game types from the CURRENT Assembly-CSharp.dll so we can see the
    up-to-date API (which methods moved where in the 2026-07-09 update).

        cd "C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods"
        powershell -ExecutionPolicy Bypass -File .\scripts\decompile-game-types.ps1
#>
$ErrorActionPreference = 'Stop'

$repo    = 'C:\Users\dhelm\source\repos\AcrossTheObelisk-Mods'
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed'
$asm     = Join-Path $managed 'Assembly-CSharp.dll'
$out     = Join-Path $repo 'refs\game-types.cs'
$proj    = Join-Path $repo 'tools\AtoDecompile'

$types = @('AtOManager','SaveManager','PlayerManager','GameManager','MatchManager')
$typeArgs = $types | ForEach-Object { "--type=$_" }

Write-Host "Decompiling game types: $($types -join ', ')`n"
dotnet run --project $proj -c Release -- $asm $out @typeArgs $managed

if (Test-Path $out) {
    Write-Host "`nWrote:" -ForegroundColor Green
    Get-Item $out | Select-Object FullName, Length | Format-List
}
