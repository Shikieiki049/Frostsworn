param([string]$GodotPath='C:/fz/godot/Godot_v4.5.1-stable_mono_win64_console.exe', [switch]$Render)
$ErrorActionPreference='Stop'
$workspace=Split-Path (Split-Path $PSScriptRoot)
$godot=$GodotPath
$info=[System.Diagnostics.ProcessStartInfo]::new($godot)
$info.UseShellExecute=$false
$info.CreateNoWindow=$true
$info.WindowStyle=[System.Diagnostics.ProcessWindowStyle]::Hidden
$info.RedirectStandardOutput=$true
$info.RedirectStandardError=$true
$info.WorkingDirectory=$workspace
$info.Environment['DOTNET_ROOT']=Join-Path $workspace 'work/dotnet9'
$info.Environment['DOTNET_ROOT_X64']=Join-Path $workspace 'work/dotnet9'
if (!$Render) { $info.ArgumentList.Add('--headless') }
@('--path',(Join-Path $PSScriptRoot 'Engine'),'--log-file',(Join-Path $workspace 'work/engine-test.log'),'--quit-after','1800') | ForEach-Object { $info.ArgumentList.Add($_) }
$process=[System.Diagnostics.Process]::Start($info)
$stdout=$process.StandardOutput.ReadToEndAsync()
$stderr=$process.StandardError.ReadToEndAsync()
if(!$process.WaitForExit(50000)) { $process.Kill(); throw 'Test host exceeded 50 seconds' }
$text=$stdout.Result + $stderr.Result
$text | Set-Content (Join-Path $workspace 'work/test-console.log')
$text -split "`n" | Where-Object { $_ -match 'PASS:|FAILED|Exception:|FROSTSWORN_TESTS| at Frostsworn|TEST_USER_DIR' } | Select-Object -Last 24
if($text -notmatch 'FROSTSWORN_TESTS_PASS') { throw 'Game-engine tests did not pass; see work/test-console.log' }
