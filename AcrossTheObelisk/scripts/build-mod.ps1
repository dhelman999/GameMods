<#
    build-mod.ps1

    Builds the workspace QuickSave mod and deploys the DLL into the active TMM profile,
    backing up the stock DLL the first time.

        cd "C:\Users\dhelm\source\repos\GameMods\AcrossTheObelisk"
        powershell -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1
#>
$ErrorActionPreference = 'Stop'

# This script lives in AcrossTheObelisk/scripts/ — project root is the parent folder.
$projectRoot = Split-Path -Parent $PSScriptRoot
$proj        = Join-Path $projectRoot 'src\QuickSave\QuickSave.csproj'
$profile     = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\AcrossTheObelisk\profiles\Default'
$pluginDir   = Join-Path $profile 'BepInEx\plugins\Binbin-Quick_Save'
$dllName     = 'com.binbin.quicksave.dll'

Write-Host "Building QuickSave (workspace build)..." -ForegroundColor Cyan
dotnet build $proj -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBUILD FAILED (see errors above). Nothing deployed." -ForegroundColor Red
    return
}

$built = Join-Path $projectRoot 'src\QuickSave\bin\Release\netstandard2.1\com.binbin.quicksave.dll'
if (-not (Test-Path $built)) { Write-Error "Built DLL not found: $built"; return }

if (-not (Test-Path $pluginDir)) { Write-Error "Plugin folder not found (install Quick_Save via TMM first): $pluginDir"; return }

$orig = Join-Path $pluginDir $dllName
$bak  = Join-Path $pluginDir ($dllName + '.stock.bak')
if ((Test-Path $orig) -and -not (Test-Path $bak)) {
    Copy-Item $orig $bak
    Write-Host "Backed up stock DLL -> $bak" -ForegroundColor DarkGray
}

Copy-Item $built $orig -Force
$builtPdb = [IO.Path]::ChangeExtension($built, 'pdb')
if (Test-Path $builtPdb) { Copy-Item $builtPdb (Join-Path $pluginDir 'com.binbin.quicksave.pdb') -Force }

Write-Host "`nDeployed workspace build -> $orig" -ForegroundColor Green
Write-Host "Launch via TMM, reproduce, then run: .\scripts\grab-log.ps1"
