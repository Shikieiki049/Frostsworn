param(
 [Parameter(Mandatory)][string]$GodotPath,
 [Parameter(Mandatory)][string]$RitsuPath,
 [string]$OutputPath,
 [switch]$Build
)
$ErrorActionPreference='Stop'
$workspace=Split-Path (Split-Path $PSScriptRoot)
if(!$OutputPath){$OutputPath=Join-Path $workspace 'work/stable-regression'}
New-Item -ItemType Directory -Force $OutputPath | Out-Null
if($Build){& (Join-Path $PSScriptRoot 'build-tests.ps1') -RitsuPath $RitsuPath}
$oldRitsu=$env:FROSTSWORN_TEST_RITSU_PATH
$oldFocus=$env:FROSTSWORN_FOCUSED_TEST
$oldPhase=$env:FROSTSWORN_STABLE_PHASE
$results=@()
try {
 $env:FROSTSWORN_TEST_RITSU_PATH=$RitsuPath
 $cases=@(@('cards','107','cards'),@('void','107','void'),@('network','107','network'),@('load','107','load'),@('mend','107','mend'),@('english','107','english'),@('audio','107','audio'),@('assets','095',''),@('vfx-cache','096',''),@('vfx','0833',''),@('shop','0821',''),@('idle','0813',''),@('hands','0828',''),@('icons','094',''),@('multiplayer-effects','0817',''),@('crystals-reports','0832',''),@('eclipse-rules','086',''),@('eclipse-damage','089',''),@('event-previews','087',''),@('event-options','088',''),@('relics-armor','083',''),@('eclipse-lobby','084',''),@('eclipse-progress','085',''),@('rebuild','0819',''),@('visual','0814',''),@('portraits','0811',''))
 foreach($case in $cases){
  $env:FROSTSWORN_FOCUSED_TEST=$case[1];$env:FROSTSWORN_STABLE_PHASE=$case[2]
  $ok=$true
  try {$null=& (Join-Path $PSScriptRoot 'run-tests.ps1') -GodotPath $GodotPath}catch{$ok=$false}
  $log=Join-Path $workspace 'work/test-console.log'
  Copy-Item -LiteralPath $log -Destination (Join-Path $OutputPath ($case[0]+'.log'))
  $lines=Get-Content -LiteralPath $log
  $errors=@($lines | Select-String -Pattern '^ERROR:|^System\..*Exception|FAILED:')
  $count=@($lines | Select-String -Pattern '^PASS:').Count
  $ok=$ok -and $errors.Count -eq 0
  $results+=@{name=$case[0];passed=$ok;assertions=$count;engineErrors=$errors.Count}
  Write-Output "$($case[0]): passed=$ok assertions=$count engineErrors=$($errors.Count)"
  if(!$ok){$lines | Select-Object -Last 12 | Write-Output}
  $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputPath 'results.json') -Encoding utf8
 }
 if(@($results | Where-Object {!$_.passed}).Count){throw 'Stable regression failed; inspect results.json and the per-group logs.'}
 Write-Output ("STABLE_REGRESSION_PASS groups={0} assertions={1}" -f $results.Count,($results | Measure-Object -Property assertions -Sum).Sum)
} finally {
 $env:FROSTSWORN_TEST_RITSU_PATH=$oldRitsu
 $env:FROSTSWORN_FOCUSED_TEST=$oldFocus
 $env:FROSTSWORN_STABLE_PHASE=$oldPhase
}
