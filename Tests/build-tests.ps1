$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$game='C:/fz/steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64'
$ritsu=Join-Path $root '../work/ritsulib-package/lib/net9.0'
$csc='C:/Program Files/dotnet/sdk/10.0.300/Roslyn/bincore/csc.dll'
$refDir=Join-Path $root '../work/net9-reference/ref/net9.0'
$common=@('/nologo','/target:library','/langversion:preview','/nullable:enable','/nowarn:1701')
$refs=Get-ChildItem $refDir -Filter *.dll | ForEach-Object { '/reference:'+$_.FullName }
$refs+=@('sts2.dll','GodotSharp.dll','0Harmony.dll') | ForEach-Object { '/reference:'+(Join-Path $game $_) }
$refs+=Get-ChildItem $ritsu -Filter *.dll | ForEach-Object { '/reference:'+$_.FullName }
$refs+='/reference:'+(Join-Path $root 'dist/Frostsworn/Frostsworn.dll')
$testSources=Get-ChildItem $PSScriptRoot -Filter *.cs | ForEach-Object { $_.FullName }
& dotnet $csc @common @refs ('/out:'+(Join-Path $PSScriptRoot 'Frostsworn.Tests.dll')) @testSources
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
$engine=Join-Path $PSScriptRoot 'Engine'
$output=Join-Path $engine '.godot/mono/temp/bin/Debug'
New-Item -ItemType Directory -Force $output | Out-Null
$analyzer=Join-Path $root '../work/godot-generators/analyzers/dotnet/cs/Godot.SourceGenerators.dll'
$config=Join-Path $engine 'test.globalconfig'
@('is_global = true',('build_property.GodotProjectDir = '+$engine),'build_property.GodotSourceGenerators = true') | Set-Content $config
& dotnet $csc @common @refs ('/analyzer:'+$analyzer) ('/analyzerconfig:'+$config) ('/out:'+(Join-Path $output 'FrostswornHarness.dll')) (Join-Path $engine 'Probe.cs')
if($LASTEXITCODE -ne 0){throw 'Godot test host compilation failed'}
'{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.7"}}}' | Set-Content (Join-Path $output 'FrostswornHarness.runtimeconfig.json')
