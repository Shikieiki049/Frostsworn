# Shared dependency resolution. Machine-specific paths belong in build.local.ps1.
function Resolve-FrostPath([string]$Value, [string]$EnvironmentName, [string]$LegacyRelative = '') {
    if ($Value) { return [IO.Path]::GetFullPath($Value) }
    $configured = [Environment]::GetEnvironmentVariable($EnvironmentName)
    if ($configured) { return [IO.Path]::GetFullPath($configured) }
    if ($LegacyRelative) {
        $candidate = Join-Path $PSScriptRoot $LegacyRelative
        if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
    }
    throw "Configure $EnvironmentName or pass its path explicitly. See BUILDING.md."
}
