param(
    [Parameter(Mandatory)][string]$Message,
    [string]$PythonPath = 'python',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (Test-Path -LiteralPath (Join-Path $root 'build.local.ps1')) { . (Join-Path $root 'build.local.ps1') }
& $PythonPath (Join-Path $PSScriptRoot 'validate_source.py')
if ($LASTEXITCODE -ne 0) { throw 'Source checks failed; nothing was published.' }
if ($ValidateOnly) { return }
function Invoke-FrostGit {
    & git -c "safe.directory=$root" -C $root @args
    if ($LASTEXITCODE -ne 0) { throw "Git command failed: $($args[0]); no success was reported." }
}
$remote = Invoke-FrostGit remote get-url origin
if ($remote -notmatch '^https://github\.com/Shikieiki049/Frostsworn(?:\.git)?$|^git@github\.com:Shikieiki049/Frostsworn(?:\.git)?$') {
    throw 'The configured origin is not the user-authorized Frostsworn repository.'
}
$branch = Invoke-FrostGit branch --show-current
if (!$branch) { throw 'Cannot publish a detached checkout.' }
Invoke-FrostGit add --all
$files = Invoke-FrostGit diff --cached --name-only
if ($files -match '\.(dll|pdb|pck|zip|log)$|(^|/)(\.godot|bin|obj)/|(^|/)\.env') {
    throw 'Generated output or local settings were staged; review before committing.'
}
if ($files) { Invoke-FrostGit commit -m $Message }
# Normal push only: conflicting remote updates fail rather than overwrite history.
Invoke-FrostGit push --set-upstream origin $branch
Write-Host "GitHub synchronized: $remote ($branch)"
