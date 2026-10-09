param(
    [string]$GamePath,
    [string]$RitsuPath,
    [string]$ReferencePath,
    [string]$PythonPath = 'python'
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'build.local.ps1')) { . (Join-Path $PSScriptRoot 'build.local.ps1') }
. (Join-Path $PSScriptRoot 'tools/build-paths.ps1')
$GamePath = Resolve-FrostPath $GamePath 'STS2_GAME_PATH'
$RitsuPath = Resolve-FrostPath $RitsuPath 'STS2_RITSU_PATH' '../../work/ritsulib-package/lib/net9.0'
& $PythonPath (Join-Path $PSScriptRoot 'tools/version_info.py')
if ($LASTEXITCODE -ne 0) { throw 'Version generation failed' }
foreach ($name in @('sts2.dll','GodotSharp.dll','0Harmony.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $GamePath "data_sts2_windows_x86_64/$name"))) { throw "Missing game dependency: $name" }
}
$framework = Join-Path $GamePath 'data_sts2_windows_x86_64'
$output = Join-Path $projectRoot 'dist/Frostsworn'
New-Item -ItemType Directory -Force $output | Out-Null
$dotnetRoot = Split-Path (Get-Command dotnet -ErrorAction Stop).Source
$compiler = Get-ChildItem (Join-Path $dotnetRoot 'sdk/*/Roslyn/bincore/csc.dll') | Sort-Object FullName | Select-Object -Last 1
if (!$compiler) { throw 'Install the .NET SDK; the runtime alone cannot compile the mod.' }
if (!$ReferencePath) {
    $legacy = Join-Path $PSScriptRoot '../work/net9-reference/ref/net9.0'
    if (Test-Path -LiteralPath $legacy) { $ReferencePath = $legacy }
    else {
        $pack = Get-ChildItem (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref/9.*/ref/net9.0') -Directory | Sort-Object FullName | Select-Object -Last 1
        if (!$pack) { throw 'Install .NET 9 SDK or pass -ReferencePath (net9.0 reference assemblies).' }
        $ReferencePath = $pack.FullName
    }
}
$refs = Get-Item -LiteralPath $ReferencePath
$arguments = [System.Collections.Generic.List[string]]::new()
@('/nologo','/target:library','/langversion:preview','/nullable:enable','/deterministic+','/debug:portable','/optimize+','/nowarn:1701','/warnaserror+',('/out:' + (Join-Path $output 'Frostsworn.dll'))) | ForEach-Object { $arguments.Add($_) }
Get-ChildItem $refs.FullName -Filter *.dll | ForEach-Object { $arguments.Add('/reference:' + $_.FullName) }
Get-ChildItem -LiteralPath $RitsuPath -Filter *.dll | ForEach-Object { $arguments.Add('/reference:' + $_.FullName) }
@('sts2.dll','GodotSharp.dll','0Harmony.dll') | ForEach-Object { $arguments.Add('/reference:' + (Join-Path $framework $_)) }
Get-ChildItem (Join-Path $projectRoot 'Source') -Filter *.cs -Recurse | ForEach-Object { $arguments.Add($_.FullName) }
& dotnet $compiler.FullName @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item (Join-Path $projectRoot 'mod_manifest.json') $output
Write-Output "Built $output/Frostsworn.dll"
