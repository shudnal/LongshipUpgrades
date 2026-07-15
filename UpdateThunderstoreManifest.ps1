param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$AssemblyVersion
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "Thunderstore manifest was not found: $ManifestPath"
}

$versionParts = $AssemblyVersion.Split('.')
if ($versionParts.Count -lt 3) {
    throw "Assembly version '$AssemblyVersion' does not contain at least three components."
}

$packageVersion = ($versionParts[0..2] -join '.')
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json

if ($manifest.version_number -eq $packageVersion) {
    Write-Host "Thunderstore manifest version is already $packageVersion"
    exit 0
}

$manifest.version_number = $packageVersion
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8
Write-Host "Updated Thunderstore manifest version to $packageVersion"
