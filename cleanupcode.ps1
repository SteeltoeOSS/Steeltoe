#Requires -Version 7.0

# This script reformats (part of) the codebase to make it compliant with our coding guidelines.

param(
    # Git branch name or base commit hash to reformat only the subset of changed files. Omit for all files.
    [string] $revision
)

function VerifySuccessExitCode {
    if ($LastExitCode -ne 0) {
        throw "Command failed with exit code $LastExitCode."
    }
}

dotnet tool restore
VerifySuccessExitCode

dotnet restore src
VerifySuccessExitCode

if ($revision) {
    $headCommitHash = git rev-parse HEAD
    VerifySuccessExitCode

    $baseCommitHash = git rev-parse $revision
    VerifySuccessExitCode

    if ($baseCommitHash -eq $headCommitHash) {
        Write-Output "Running code cleanup on staged/unstaged files."
        dotnet regitlint -s src/Steeltoe.All.sln --print-command --skip-tool-check --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN -f staged,modified
        VerifySuccessExitCode
    }
    else {
        Write-Output "Running code cleanup on commit range $baseCommitHash..$headCommitHash, including staged/unstaged files."
        dotnet regitlint -s src/Steeltoe.All.sln --print-command --skip-tool-check --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN -f staged,modified,commits -a $headCommitHash -b $baseCommitHash
        VerifySuccessExitCode
    }
}
else {
    Write-Output "Running code cleanup on all files."
    dotnet regitlint -s src/Steeltoe.All.sln --print-command --skip-tool-check --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN
    VerifySuccessExitCode
}
