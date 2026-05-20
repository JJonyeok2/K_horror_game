$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$repoRoot = Resolve-Path (Join-Path $projectRoot "..\\..")
$source = Join-Path $repoRoot "assets\\external\\ambientcg\\materials"
$target = Join-Path $projectRoot "Assets\\External\\ambientcg\\materials"

if (-not (Test-Path -LiteralPath $source)) {
    throw "Source ambientCG materials not found: $source"
}

New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
if (Test-Path -LiteralPath $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}

Copy-Item -Path $source -Destination $target -Recurse -Force
Write-Output "Copied ambientCG materials to $target"
