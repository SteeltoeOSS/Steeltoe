param(
    [Parameter(Mandatory = $true)][string]$CoverageFile,
    [Parameter(Mandatory = $true)][string]$CanonicalPrefix
)

# Each dotnet build/publish subprocess spawned by GitProperties.Build.Test runs against its own
# temporary copy of the repo, so the PDB (and therefore the coverage report) embeds an ephemeral
# temp path per test run instead of the real checkout path. Coverage tools that merge hits by class
# name (e.g. ReportGenerator) tolerate this, but SonarCloud correlates coverage by file path, so
# without this rewrite it can't attribute any of these hits to the real source files.
$pattern = '[^"]*[\\/]src[\\/]Management[\\/]src[\\/]GitProperties\.Build[\\/]'
$replacement = "$CanonicalPrefix/"

$content = Get-Content -Path $CoverageFile -Raw
$content = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)
Set-Content -Path $CoverageFile -Value $content -NoNewline
