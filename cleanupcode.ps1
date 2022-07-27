#Requires -Version 7.0

# This script reformats the entire codebase to make it compliant with our coding guidelines.

param(
    # Branch name or commit hash
    [string] $diff
    # TODO: With staged/unstaged
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

    echo "Using commit range for cleanup: $baseCommitHash..$headCommitHash"

    #dotnet regitlint -s Steeltoe.All.sln --print-command --skip-tool-check --disable-jb-path-hack --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN -f commits -a $headCommitHash -b $baseCommitHash
    #VerifySuccessExitCode
}
else {
    #dotnet regitlint -s Steeltoe.All.sln --print-command --skip-tool-check --disable-jb-path-hack --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN
    #VerifySuccessExitCode
}
