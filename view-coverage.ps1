<#
.SYNOPSIS
    Collects coverage for GitProperties.Build.Test and renders a line-by-line HTML report.

.DESCRIPTION
    Mirrors what CI does (dotnet-coverage wraps the whole dotnet build/publish subprocess tree spawned by the
    tests, then normalize-coverage-paths.ps1 rewrites ephemeral temp paths and merges duplicate subprocess
    entries), then renders the result with ReportGenerator for local inspection.

    Each run writes into its own timestamped directory under <OutputBasePath>/coveragereport/, so you can keep
    several runs around and diff them against each other later.

    One-time setup (not done by this script):
        dotnet tool install --global dotnet-reportgenerator-globaltool
        dotnet tool restore

.PARAMETER OutputBasePath
    Base directory for test output and the generated report. Defaults to "C:\Temp".

.PARAMETER TargetDir
    Directory to write the HTML report into. Defaults to "<OutputBasePath>/coveragereport/<current date/time>".

.EXAMPLE
    ./view-coverage.ps1

.EXAMPLE
    ./view-coverage.ps1 -OutputBasePath D:\CoverageRuns

.EXAMPLE
    ./view-coverage.ps1 -TargetDir coveragereport/baseline
#>
param(
    [string]$OutputBasePath = "C:\Temp",
    [string]$TargetDir = (Join-Path $OutputBasePath "coveragereport_$(Get-Date -Format 'yyyy-MM-dd_HHmmss')")
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = $PSScriptRoot
$testProject = Join-Path $repoRoot "src/Management/test/GitProperties.Build.Test"
$testOutput = Join-Path $OutputBasePath "TestOutput"
$coverageFile = Join-Path $testOutput "GitProperties.Build.Test.cobertura.xml"

New-Item -ItemType Directory -Force -Path $testOutput | Out-Null

Write-Host "==> Building test project" -ForegroundColor Cyan
dotnet build $testProject -c Release

Write-Host "==> Collecting coverage (this spawns real dotnet build/publish subprocesses, can take a minute or two)" -ForegroundColor Cyan
dotnet-coverage collect -f cobertura -o $coverageFile -- `
    dotnet test $testProject --no-build --configuration Release `
    --logger trx --results-directory $testOutput

Write-Host "==> Normalizing temp paths and merging duplicate subprocess entries" -ForegroundColor Cyan
& (Join-Path $repoRoot ".github/scripts/normalize-coverage-paths.ps1") `
    -CoverageFile $coverageFile `
    -RepoRoot $repoRoot `
    -ProjectRelativePath "src/Management/src/GitProperties.Build"

Write-Host "==> Generating HTML report" -ForegroundColor Cyan
reportgenerator -reports:$coverageFile -targetdir:$TargetDir -reporttypes:Html -filefilters:"-*.g.cs"

$indexPath = [System.IO.Path]::GetFullPath((Join-Path $TargetDir "index.html"))
$indexUri = ([System.Uri]$indexPath).AbsoluteUri
Write-Host "==> Report ready: $indexUri" -ForegroundColor Cyan
