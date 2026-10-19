param([Parameter(Mandatory)][string]$GodotPath,[Parameter(Mandatory)][string]$ModDll,[Parameter(Mandatory)][string]$RitsuPath,[Parameter(Mandatory)][string]$ReleaseInfo)
$ErrorActionPreference='Stop'
# Standalone Godot gets a temporary copy of the real release metadata beside its
# executable, exactly where production reads it. Preserve any prior file bytes.
$releaseTarget=Join-Path (Split-Path $GodotPath) 'release_info.json'
$existed=Test-Path -LiteralPath $releaseTarget
$prior=if($existed){[IO.File]::ReadAllBytes($releaseTarget)}else{$null}
$oldCross=$env:FROSTSWORN_CROSS_DLL;$oldRitsu=$env:FROSTSWORN_TEST_RITSU_PATH
try {
 Copy-Item -LiteralPath $ReleaseInfo -Destination $releaseTarget
 $env:FROSTSWORN_CROSS_DLL=(Resolve-Path -LiteralPath $ModDll).Path
 $env:FROSTSWORN_TEST_RITSU_PATH=(Resolve-Path -LiteralPath $RitsuPath).Path
 & (Join-Path $PSScriptRoot 'run-tests.ps1') -GodotPath $GodotPath
} finally {
 if($existed){[IO.File]::WriteAllBytes($releaseTarget,$prior)}else{Remove-Item -LiteralPath $releaseTarget}
 $env:FROSTSWORN_CROSS_DLL=$oldCross;$env:FROSTSWORN_TEST_RITSU_PATH=$oldRitsu
}
