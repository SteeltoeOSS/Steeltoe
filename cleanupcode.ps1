#Requires -Version 7.4

# This script reformats (part of) the codebase to make it compliant with our coding guidelines.

param(
    # Git branch name or base commit hash to reformat only the subset of changed files. Omit for all files.
    [string] $revision
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
$solutionFile = 'src/Steeltoe.All.sln'

dotnet tool restore
dotnet restore $solutionFile /p:NuGetAudit=false
dotnet build $solutionFile --no-restore --configuration Release /p:RunAnalyzers=false

if ($revision) {
    $headCommitHash = git rev-parse HEAD
    $baseCommitHash = git rev-parse $revision

    if ($baseCommitHash -eq $headCommitHash) {
        Write-Output "Running code cleanup on staged/unstaged files."
        dotnet jb cleanupcode --version
        dotnet regitlint -s $solutionFile --print-command --skip-tool-check --max-runs=5 --jb --dotnetcoresdk=$(dotnet --version) --jb-profile="Steeltoe Full Cleanup" --jb --no-updates --jb --properties:Configuration=Release --jb --properties:RunAnalyzers=false --jb --properties:NuGetAudit=false --jb --verbosity=WARN -f staged,modified
    }
    else {
        Write-Output "Running code cleanup on commit range $baseCommitHash..$headCommitHash, including staged/unstaged files."
        dotnet jb cleanupcode --version
        dotnet regitlint -s $solutionFile --print-command --skip-tool-check --max-runs=5 --jb --dotnetcoresdk=$(dotnet --version) --jb-profile="Steeltoe Full Cleanup" --jb --no-updates --jb --properties:Configuration=Release --jb --properties:RunAnalyzers=false --jb --properties:NuGetAudit=false --jb --verbosity=WARN -f staged,modified,commits -a $headCommitHash -b $baseCommitHash
    }
}
else {
    Write-Output "Running code cleanup on all files."
    dotnet jb cleanupcode --version
    dotnet regitlint -s $solutionFile --print-command --skip-tool-check --jb --dotnetcoresdk=$(dotnet --version) --jb-profile="Steeltoe Full Cleanup" --jb --no-updates --jb --properties:Configuration=Release --jb --properties:RunAnalyzers=false --jb --properties:NuGetAudit=false --jb --verbosity=WARN
}
