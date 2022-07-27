#Requires -Version 7.0

# This script reformats the entire codebase to make it compliant with our coding guidelines.

param(
    # Branch name or base commit hash to reformat only the subset of changed files. Leave empty for all files.
    [string] $diff
)

function VerifySuccessExitCode {
    if ($LastExitCode -ne 0) {
        throw "Command failed with exit code $LastExitCode."
    }
}

dotnet tool restore
VerifySuccessExitCode

dotnet restore
VerifySuccessExitCode

if ($diff) {
    $headCommitHash = git rev-parse HEAD
    VerifySuccessExitCode

    $baseCommitHash = git rev-parse $diff
    VerifySuccessExitCode

    echo "Using commit range for cleanup: $baseCommitHash..$headCommitHash, including staged/unstaged files."

    dotnet regitlint -s Steeltoe.All.sln --print-command --skip-tool-check --disable-jb-path-hack --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN -f staged,modified,commits -a $headCommitHash -b $baseCommitHash
    VerifySuccessExitCode
}
else {
    echo "Running cleanup on all files."

    dotnet regitlint -s Steeltoe.All.sln --print-command --skip-tool-check --disable-jb-path-hack --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN
    VerifySuccessExitCode
}
