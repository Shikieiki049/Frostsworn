param(
    [string]$PythonPath = 'python',
    [string]$GodotPath,
    [string]$GamePath,
    [string]$RitsuPath,
    [string]$ReferencePath
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'build.local.ps1')) { . (Join-Path $PSScriptRoot 'build.local.ps1') }
. (Join-Path $PSScriptRoot 'tools/build-paths.ps1')
$GodotPath = Resolve-FrostPath $GodotPath 'GODOT_PATH'
if (!(Get-Command $PythonPath -ErrorAction SilentlyContinue)) { throw "Missing Python: $PythonPath" }
& $PythonPath (Join-Path $PSScriptRoot 'tools/expansion_content.py')
if ($LASTEXITCODE -ne 0) { throw 'Card generation failed' }
& $PythonPath (Join-Path $PSScriptRoot 'tools/make_assets.py')
if ($LASTEXITCODE -ne 0) { throw 'Localization generation failed' }
& $PythonPath (Join-Path $PSScriptRoot 'tools/relic_small_icons.py')
if ($LASTEXITCODE -ne 0) { throw 'Relic icon generation failed' }
& (Join-Path $PSScriptRoot 'build.ps1') -GamePath $GamePath -RitsuPath $RitsuPath -ReferencePath $ReferencePath -PythonPath $PythonPath
$assetPath = Join-Path $PSScriptRoot 'Assets'
$logPath = Join-Path $PSScriptRoot 'rebuild-import.log'
& $GodotPath --headless --editor --import --path $assetPath --log-file (Join-Path $PSScriptRoot 'rebuild-import-engine.log') --quit *> $logPath
if ($LASTEXITCODE -ne 0) { throw "Resource import failed; see $logPath" }
$packLog = Join-Path $PSScriptRoot 'rebuild-pack.log'
& $GodotPath --headless --editor --path $assetPath --log-file (Join-Path $PSScriptRoot 'rebuild-pack-engine.log') --script pack.gd --quit-after 20 *> $packLog
if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $packLog -Pattern 'FROSTSWORN_PACK_RESULT=0' -Quiet)) {
    throw "Resource packing failed; see $packLog"
}
Write-Host 'Done. Copy the files in dist/Frostsworn to the game mods/Frostsworn folder after closing the game.'
